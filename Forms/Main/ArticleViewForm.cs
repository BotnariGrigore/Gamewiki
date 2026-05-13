using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Services;
using GameWikiApp.Models;
using GameWikiApp.Data;

namespace GameWikiApp.Forms.Main
{
    public class ArticleViewForm : Form
    {
        private readonly int _articleId;
        private readonly ArticleService _articleService = new();
        private readonly CommentService _commentService = new();
        private readonly LikeService _likeService = new();
        private readonly SavedArticleService _savedArticleService = new();
        private readonly UserRepository _userRepo = new();

        private PictureBox pbCover;
        private Label lblTitle;
        private Label lblMeta;
        private RichTextBox txtContent;
        private Button btnLike;
        private Button btnSave;
        private Label lblLikeCount;
        private Label lblSaveStatus;
        private TextBox txtComment;
        private Button btnComment;
        private FlowLayoutPanel flpComments;
        private Label commentsLabel;

        private bool _isLiked;
        private bool _isSaved;

        public ArticleViewForm(int articleId)
        {
            _articleId = articleId;
            Text = "Article";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(960, 720);
            MinimumSize = new Size(700, 500);

            InitializeLayout();
            _ = LoadContentAsync();
        }

        public ArticleViewForm(WikiArticle article) : this(article.ArticleId) { }

        private void InitializeLayout()
        {
            // Title
            lblTitle = new Label
            {
                Location = new Point(20, 16),
                Size = new Size(ClientSize.Width - 40, 40),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblTitle);

            // Meta info
            lblMeta = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, null, 20, 60);
            Controls.Add(lblMeta);

            // Separator
            Controls.Add(ThemeHelper.CreateSeparator(ClientSize.Width - 40, 20, 85));

            // Cover
            pbCover = new PictureBox
            {
                Location = new Point(20, 96),
                Size = new Size(ClientSize.Width - 40, 240),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(pbCover);

            // Content
            txtContent = new RichTextBox
            {
                Location = new Point(20, 348),
                Size = new Size(ClientSize.Width - 40, 200),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Segoe UI", 11),
                WordWrap = true
            };
            Controls.Add(txtContent);

            // Action bar
            var actionBar = new Panel
            {
                Location = new Point(20, 558),
                Size = new Size(ClientSize.Width - 40, 48)
            };

            btnLike = new Button
            {
                Text = "Like",
                Location = new Point(0, 8),
                Size = new Size(120, 32),
                UseVisualStyleBackColor = true
            };
            btnLike.Click += async (_, __) =>
            {
                if (!SessionManager.IsAuthenticated)
                {
                    MessageBox.Show("Please sign in to like articles.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var ok = await _likeService.ToggleLikeAsync(_articleId, SessionManager.CurrentUser!.UserId);
                await UpdateLikeStatus();
            };
            actionBar.Controls.Add(btnLike);

            lblLikeCount = ThemeHelper.CreateLabel("0 likes", 9, FontStyle.Regular, null, 130, 14);
            actionBar.Controls.Add(lblLikeCount);

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(240, 8),
                Size = new Size(120, 32),
                UseVisualStyleBackColor = true
            };
            btnSave.Click += async (_, __) =>
            {
                if (!SessionManager.IsAuthenticated)
                {
                    MessageBox.Show("Please sign in to save articles.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var ok = await _savedArticleService.ToggleSaveAsync(_articleId, SessionManager.CurrentUser!.UserId);
                _isSaved = !await _savedArticleService.IsSavedAsync(_articleId, SessionManager.CurrentUser!.UserId);
                btnSave.Text = _isSaved ? "Unsave" : "Save";
                lblSaveStatus.Visible = true;
                lblSaveStatus.Text = _isSaved ? "Saved!" : "Removed from saved";
            };
            actionBar.Controls.Add(btnSave);

            lblSaveStatus = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, null, 370, 14);
            actionBar.Controls.Add(lblSaveStatus);

            Controls.Add(actionBar);

            // Comments section
            commentsLabel = ThemeHelper.CreateLabel("Comments", 14, FontStyle.Bold, null, 20, 0);
            commentsLabel.Location = new Point(20, actionBar.Bottom + 20);
            Controls.Add(commentsLabel);

            var commentRow = new Panel
            {
                Location = new Point(20, commentsLabel.Bottom + 10),
                Size = new Size(ClientSize.Width - 40, 36)
            };

            txtComment = new TextBox
            {
                PlaceholderText = "Write a comment...",
                Location = new Point(0, 0),
                Size = new Size(commentRow.Width - 90, 34)
            };
            txtComment.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                { e.SuppressKeyPress = true; _ = PostCommentAsync(); }
            };
            var wrap = ThemeHelper.WrapInput(txtComment, commentRow.Width - 86, 36);
            commentRow.Controls.Add(wrap);

            btnComment = ThemeHelper.CreateThemedButton("Post", commentRow.Width - 80, 0, 70, 30);
            btnComment.Click += (_, __) => _ = PostCommentAsync();
            commentRow.Controls.Add(btnComment);

            Controls.Add(commentRow);

            flpComments = new FlowLayoutPanel
            {
                Location = new Point(20, commentRow.Bottom + 10),
                Size = new Size(ClientSize.Width - 40, ClientSize.Height - commentRow.Bottom - 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            Controls.Add(flpComments);

            Resize += (_, __) =>
            {
                lblTitle.Size = new Size(ClientSize.Width - 40, 40);
                pbCover.Size = new Size(ClientSize.Width - 40, 240);
                txtContent.Size = new Size(ClientSize.Width - 40, 180);
                actionBar.Size = new Size(ClientSize.Width - 40, 48);
                commentRow.Size = new Size(ClientSize.Width - 40, 36);
                commentRow.Location = new Point(20, commentsLabel.Bottom + 10);
                wrap.Size = new Size(commentRow.Width - 86, 34);
                flpComments.Location = new Point(20, commentRow.Bottom + 10);
                flpComments.Size = new Size(ClientSize.Width - 40, ClientSize.Height - commentRow.Bottom - 20);
            };
        }

        private async Task LoadContentAsync()
        {
            try
            {
                var article = await _articleService.GetByIdAsync(_articleId);
                if (article == null)
                {
                    MessageBox.Show("Article not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                var user = await _userRepo.GetByIdAsync(article.AuthorId);
                lblTitle.Text = article.Title;
                lblMeta.Text = $"By @{user?.Username ?? "Unknown"} · {article.CreatedAt:MMMM dd, yyyy} · {article.ViewsCount} views" +
                               (article.IsPublished ? "" : " [DRAFT]");

                txtContent.Text = article.Content;

                if (!string.IsNullOrEmpty(article.CoverImage))
                {
                    try
                    {
                        var path = article.CoverImage.Replace('/', Path.DirectorySeparatorChar);
                        var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                        if (File.Exists(local))
                            pbCover.Image = Image.FromFile(local);
                        else
                            pbCover.Image = null;
                    }
                    catch { pbCover.Image = null; }
                }

                if (SessionManager.IsAuthenticated)
                {
                    _isLiked = await _likeService.HasLikedAsync(_articleId, SessionManager.CurrentUser!.UserId);
                    _isSaved = await _savedArticleService.IsSavedAsync(_articleId, SessionManager.CurrentUser!.UserId);
                    btnLike.Text = _isLiked ? "Liked" : "Like";
                    btnSave.Text = _isSaved ? "Unsave" : "Save";
                }

                await UpdateLikeStatus();
                await LoadCommentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load article: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateLikeStatus()
        {
            var count = await _likeService.GetCountAsync(_articleId);
            lblLikeCount.Text = $"{count} likes";
            btnLike.Text = _isLiked ? "Liked" : "Like";
        }

        private async Task LoadCommentsAsync()
        {
            try
            {
                flpComments.Controls.Clear();
                var comments = await _commentService.GetByArticleIdAsync(_articleId);

                foreach (var c in comments)
                {
                    var card = ThemeHelper.CreateCardPanel(flpComments.Width - 10, 60);

                    var lblUser = ThemeHelper.CreateLabel(c.Username, 9.5f, FontStyle.Bold, null, 10, 5);
                    card.Controls.Add(lblUser);

                    var lblTime = ThemeHelper.CreateLabel(c.CreatedAt.ToString("MMM dd, HH:mm"), 8, FontStyle.Regular, null, 10, 24);
                    card.Controls.Add(lblTime);

                    var lblComment = new Label
                    {
                        Text = c.CommentText,
                        Location = new Point(12, 42),
                        Size = new Size(card.Width - 50, 16),
                        AutoSize = true,
                        MaximumSize = new Size(card.Width - 36, 100)
                    };
                    card.Controls.Add(lblComment);

                    flpComments.Controls.Add(card);
                }

                if (!comments.Any())
                {
                    flpComments.Controls.Add(new Label
                    {
                        Text = "No comments yet. Be the first to comment!",
                        AutoSize = true,
                        Margin = new Padding(12)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load comments: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PostCommentAsync()
        {
            if (!SessionManager.IsAuthenticated)
            {
                MessageBox.Show("Please sign in to comment.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var text = txtComment.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                await _commentService.CreateAsync(new Comment
                {
                    ArticleId = _articleId,
                    UserId = SessionManager.CurrentUser!.UserId,
                    CommentText = text
                });

                txtComment.Clear();
                await LoadCommentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to post comment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}