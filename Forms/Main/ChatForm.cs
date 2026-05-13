using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameWikiApp.Helpers;
using GameWikiApp.Services;
using GameWikiApp.Models;
using GameWikiApp.Data;
using Message = GameWikiApp.Models.Message;

namespace GameWikiApp.Forms.Main
{
    public class ChatForm : Form
    {
        private Panel sidebarPanel;
        private Panel mainChatPanel;
        private FlowLayoutPanel flpConversations;
        private TextBox txtNewMessage;
        private Button btnSend;
        private Label lblChatTitle;
        private ListView lstConversations;
        private Label lblNoConversations;

        private readonly ConversationService _conversationService = new();
        private readonly UserService _userService = new();
        private readonly ChatService _chatService = new();

        private List<Conversation> _conversations = new();
        private int? _activeConversationId;
        private int? _directMessageTargetId;

        public ChatForm()
        {
            Text = "Chat";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(900, 600);
            MinimumSize = new Size(650, 450);

            InitializeLayout();
            _ = LoadConversationsAsync();
        }

        public ChatForm(int friendId) : this()
        {
            _directMessageTargetId = friendId;
        }

        private void InitializeLayout()
        {
            // Sidebar - conversation list
            sidebarPanel = new Panel
            {
                Width = 280,
                Dock = DockStyle.Left,
                Padding = new Padding(12)
            };
            Controls.Add(sidebarPanel);

            // Header with title and button on same row
            var sidebarHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(280, 36)
            };

            var lblConvTitle = ThemeHelper.CreateLabel("Conversations", 13, FontStyle.Bold, null, 0, 8);
            sidebarHeader.Controls.Add(lblConvTitle);

            var btnNewConv = ThemeHelper.CreateThemedButton("New", 170, 4, 100, 28);
            btnNewConv.Click += (_, __) => OpenNewConversation();
            sidebarHeader.Controls.Add(btnNewConv);

            sidebarPanel.Controls.Add(sidebarHeader);

            // Search box in sidebar
            var txtSearchConv = new TextBox
            {
                PlaceholderText = "Search chats...",
                Location = new Point(0, 42),
                Size = new Size(256, 34)
            };
            var searchWrap = ThemeHelper.WrapInput(txtSearchConv, 260, 36);
            searchWrap.Location = new Point(8, 42);
            sidebarPanel.Controls.Add(searchWrap);

            // Conversation list - fills remaining space
            lstConversations = new ListView
            {
                Location = new Point(8, 86),
                Size = new Size(264, 380),
                View = View.List,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            lstConversations.SelectedIndexChanged += (_, __) =>
            {
                if (lstConversations.SelectedItems.Count > 0)
                {
                    var convId = (int)lstConversations.SelectedItems[0].Tag;
                    _ = LoadConversationAsync(convId);
                }
            };
            sidebarPanel.Controls.Add(lstConversations);

            lblNoConversations = ThemeHelper.CreateLabel("No conversations yet.\nStart a new chat!", 9, FontStyle.Italic, null, 30, 180);
            lblNoConversations.TextAlign = ContentAlignment.MiddleCenter;
            lblNoConversations.Size = new Size(200, 60);
            lblNoConversations.Visible = false;
            sidebarPanel.Controls.Add(lblNoConversations);

            // Divider
            var divider = new Panel
            {
                Width = 1,
                Dock = DockStyle.Left
            };
            Controls.Add(divider);

            // Main chat area
            mainChatPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            Controls.Add(mainChatPanel);

            // Chat header bar
            var chatHeader = new Panel
            {
                Height = 48,
                Dock = DockStyle.Top
            };
            mainChatPanel.Controls.Add(chatHeader);

            lblChatTitle = ThemeHelper.CreateLabel("Select a conversation", 14, FontStyle.Bold, null, 16, 14);
            chatHeader.Controls.Add(lblChatTitle);

            // Message display area
            var messageBox = new RichTextBox
            {
                Location = new Point(0, 48),
                Size = new Size(mainChatPanel.Width, mainChatPanel.Height - 100),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(16)
            };
            messageBox.Name = "rtbMessages";
            mainChatPanel.Controls.Add(messageBox);

            // Input area - fixed at bottom
            var inputPanel = new Panel
            {
                Height = 52,
                Dock = DockStyle.Bottom,
                Padding = new Padding(12, 8, 12, 8)
            };
            mainChatPanel.Controls.Add(inputPanel);

            txtNewMessage = new TextBox
            {
                Location = new Point(12, 10),
                Size = new Size(mainChatPanel.Width - 104, 34),
                Multiline = true,
                ScrollBars = ScrollBars.None,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Type a message...",
                MaximumSize = new Size(mainChatPanel.Width - 104, 34)
            };
            txtNewMessage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                { e.SuppressKeyPress = true; _ = SendMessageAsync(); }
            };
            inputPanel.Controls.Add(txtNewMessage);

            btnSend = ThemeHelper.CreateThemedButton("Send", mainChatPanel.Width - 84, 10, 72, 34);
            btnSend.Anchor = AnchorStyles.Right;
            btnSend.Click += (_, __) => _ = SendMessageAsync();
            inputPanel.Controls.Add(btnSend);

            // Responsive resize handler
            mainChatPanel.Resize += (_, __) =>
            {
                if (messageBox != null && inputPanel != null)
                {
                    messageBox.Size = new Size(mainChatPanel.Width, mainChatPanel.Height - 100);
                    txtNewMessage.Size = new Size(mainChatPanel.Width - 104, 34);
                    btnSend.Location = new Point(mainChatPanel.Width - 84, 10);
                }
            };
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                _conversations = (await _conversationService.GetByUserIdAsync(SessionManager.CurrentUser!.UserId)).ToList();
                lstConversations.Items.Clear();

                if (!_conversations.Any())
                {
                    lblNoConversations.Visible = true;
                    return;
                }

                lblNoConversations.Visible = false;

                foreach (var c in _conversations)
                {
                    var item = new ListViewItem
                    {
                        Text = c.ConversationName ?? (c.IsGroupChat ? "Group Chat" : "Direct Message"),
                        Tag = c.ConversationId
                    };
                    lstConversations.Items.Add(item);
                }

                if (_directMessageTargetId.HasValue)
                {
                    var dmConv = _conversations.FirstOrDefault(c => !c.IsGroupChat);
                    if (dmConv != null)
                    {
                        for (int i = 0; i < lstConversations.Items.Count; i++)
                        {
                            if ((int)lstConversations.Items[i].Tag == dmConv.ConversationId)
                            {
                                lstConversations.Items[i].Selected = true;
                                _ = LoadConversationAsync(dmConv.ConversationId);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load conversations: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadConversationAsync(int conversationId)
        {
            try
            {
                _activeConversationId = conversationId;
                var conv = _conversations.FirstOrDefault(c => c.ConversationId == conversationId);
                lblChatTitle.Text = conv?.ConversationName ?? "Conversation";

                var participants = await _conversationService.GetParticipantsAsync(conversationId);
                var messages = await _chatService.GetMessagesAsync(conversationId);
                var currentUser = SessionManager.CurrentUser!;

                var rtb = (RichTextBox)Controls.Find("rtbMessages", true).FirstOrDefault();
                if (rtb == null) return;
                rtb.Clear();

                rtb.SelectionFont = new Font("Segoe UI", 9, FontStyle.Italic);
                rtb.AppendText("Participants: " +
                    string.Join(", ", participants.Select(p => p.Username)) + "\n\n");

                foreach (var msg in messages)
                {
                    var sender = participants.FirstOrDefault(p => p.UserId == msg.SenderId);
                    var senderName = sender?.Username ?? "Unknown";
                    var isMe = msg.SenderId == currentUser.UserId;

                    rtb.SelectionFont = new Font("Segoe UI", 7.5f);
                    rtb.AppendText($"[{msg.SentAt:HH:mm}] ");

                    rtb.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
                    rtb.AppendText(senderName + ": ");

                    rtb.SelectionFont = new Font("Segoe UI", 10);
                    rtb.AppendText((msg.MessageText ?? "(image)") + "\n\n");
                }

                rtb.SelectionStart = rtb.TextLength;
                rtb.ScrollToCaret();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load conversation: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendMessageAsync()
        {
            if (!_activeConversationId.HasValue)
            {
                MessageBox.Show("No conversation selected.", "Chat",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var text = txtNewMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                var msg = new GameWikiApp.Models.Message
                {
                    ConversationId = _activeConversationId.Value,
                    SenderId = SessionManager.CurrentUser!.UserId,
                    MessageText = text
                };

                await _chatService.SendMessageAsync(msg);
                txtNewMessage.Clear();

                await LoadConversationAsync(_activeConversationId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send message: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenNewConversation()
        {
            using var dlg = new NewConversationDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _ = LoadConversationsAsync();
            }
        }

        private class NewConversationDialog : Form
        {
            private TextBox txtUsername;
            private Button btnCreate;

            public NewConversationDialog()
            {
                Text = "New Conversation";
                StartPosition = FormStartPosition.CenterParent;
                Size = new Size(380, 200);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                Controls.Add(ThemeHelper.CreateLabel("Enter username to chat with:", 10, FontStyle.Regular, null, 12, 12));

                txtUsername = new TextBox { Size = new Size(320, 34), Location = new Point(12, 38) };
                var wrap = ThemeHelper.WrapInput(txtUsername, 326, 38);
                wrap.Location = new Point(12, 36);
                Controls.Add(wrap);

                btnCreate = ThemeHelper.CreateThemedButton("Start Chat", 12, 84, 150, 36);
                btnCreate.Click += (_, __) =>
                {
                    DialogResult = DialogResult.OK;
                    Close();
                };
                Controls.Add(btnCreate);
            }
        }
    }
}