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

namespace GameWikiApp.Forms.Main
{
    public class FriendsForm : Form
    {
        private Panel mainPanel;
        private FlowLayoutPanel flpFriends;
        private TextBox txtSearchFriend;
        private TextBox txtAddUsername;
        private Label lblTitle;
        private TabControl tabControl;

        private readonly FriendService _friendService = new();
        private readonly UserRepository _userRepo = new();

        private List<Friend> _friends = new();

        public FriendsForm()
        {
            Text = "Friends";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(800, 600);
            MinimumSize = new Size(600, 450);

            InitializeLayout();
            _ = LoadFriendsAsync();
        }

        private void InitializeLayout()
        {
            // Header
            var header = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                Padding = new Padding(20, 12, 20, 12)
            };
            Controls.Add(header);

            lblTitle = ThemeHelper.CreateLabel("Friends", 20, FontStyle.Bold, null, 0, 8);
            header.Controls.Add(lblTitle);

            // Tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 240
            };

            var tabFriends = new TabPage("Friends");
            var tabAddFriend = new TabPage("Add Friend");

            // -- Friends tab --
            var pnlFriends = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };
            tabFriends.Controls.Add(pnlFriends);

            var searchRow = new Panel
            {
                Height = 38,
                Dock = DockStyle.Top
            };
            pnlFriends.Controls.Add(searchRow);

            txtSearchFriend = new TextBox
            {
                PlaceholderText = "Search friends...",
                Size = new Size(300, 34)
            };
            txtSearchFriend.TextChanged += async (_, __) => await FilterFriends();
            var wrapper = ThemeHelper.WrapInput(txtSearchFriend, 306, 38);
            searchRow.Controls.Add(wrapper);

            flpFriends = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            pnlFriends.Controls.Add(flpFriends);

            // -- Add Friend tab --
            var pnlAdd = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };
            tabAddFriend.Controls.Add(pnlAdd);

            pnlAdd.Controls.Add(ThemeHelper.CreateLabel("Add a Friend by Username", 14, FontStyle.Bold, null, 0, 10));
            pnlAdd.Controls.Add(ThemeHelper.CreateLabel("Enter the username of the person you'd like to add as a friend.", 9.5f, FontStyle.Regular, null, 0, 38));

            var lblUsername = ThemeHelper.CreateLabel("Username:", 10, FontStyle.Bold, null, 0, 75);
            pnlAdd.Controls.Add(lblUsername);

            txtAddUsername = new TextBox
            {
                Size = new Size(350, 36),
                PlaceholderText = "Enter username..."
            };
            var wrapUser = ThemeHelper.WrapInput(txtAddUsername, 356, 40);
            wrapUser.Location = new Point(0, 100);
            pnlAdd.Controls.Add(wrapUser);

            var btnAdd = ThemeHelper.CreateThemedButton("Send Friend Request", 0, 155, 200, 38);
            btnAdd.Click += async (_, __) => await AddFriendAsync();
            pnlAdd.Controls.Add(btnAdd);

            var lblStatus = ThemeHelper.CreateLabel("", 9, FontStyle.Regular, null, 0, 205);
            lblStatus.Name = "lblStatus";
            pnlAdd.Controls.Add(lblStatus);

            tabControl.TabPages.Add(tabFriends);
            tabControl.TabPages.Add(tabAddFriend);
            Controls.Add(tabControl);

            var footer = new Panel
            {
                Height = 40,
                Dock = DockStyle.Bottom
            };
            Controls.Add(footer);

            var lblCount = new Label
            {
                Text = "0 friends",
                Location = new Point(12, 10),
                AutoSize = true
            };
            lblCount.Name = "lblCount";
            footer.Controls.Add(lblCount);
        }

        private async Task LoadFriendsAsync()
        {
            try
            {
                _friends = (await _friendService.GetFriendsAsync(SessionManager.CurrentUser!.UserId)).ToList();
                await FilterFriends();

                var lblCount = (Label)Controls.Find("lblCount", true).FirstOrDefault();
                    if (lblCount != null) lblCount.Text = $"{_friends.Count(f => f.Status == Models.FriendStatus.Accepted)} friends";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load friends: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task FilterFriends()
        {
            flpFriends.Controls.Clear();
            var query = txtSearchFriend.Text.Trim().ToLower();

            var filtered = _friends.Where(f =>
                f.Status == Models.FriendStatus.Accepted &&
                (string.IsNullOrEmpty(query) ||
                 GetFriendUsername(f).ToLower().Contains(query))
            ).ToList();

            foreach (var f in filtered)
            {
                flpFriends.Controls.Add(CreateFriendCard(f));
            }

            if (!filtered.Any())
            {
                flpFriends.Controls.Add(new Label
                {
                    Text = string.IsNullOrEmpty(query) ? "No friends yet. Add some!" : "No matching friends found.",
                    AutoSize = true,
                    Margin = new Padding(12)
                });
            }
        }

        private string GetFriendUsername(Friend f)
        {
            if (f.UserId == SessionManager.CurrentUser!.UserId)
                return f.FriendId.ToString();
            return f.UserId.ToString();
        }

        private Panel CreateFriendCard(Friend f)
        {
            var card = ThemeHelper.CreateCardPanel(500, 70);

            var otherId = f.UserId == SessionManager.CurrentUser!.UserId ? f.FriendId : f.UserId;

            var lbl = ThemeHelper.CreateLabel($"User #{otherId}", 11, FontStyle.Bold, null, 12, 18);
            card.Controls.Add(lbl);

            var lblSince = ThemeHelper.CreateLabel($"Friends since {f.CreatedAt:MMM dd, yyyy}", 8.5f, FontStyle.Regular, null, 12, 42);
            card.Controls.Add(lblSince);

            var btnMessage = ThemeHelper.CreateThemedButton("Message", card.Width - 170, 16, 80, 32);
            btnMessage.Click += (_, __) => OpenChatWithUser(friendId: otherId);
            card.Controls.Add(btnMessage);

            return card;
        }

        private async Task AddFriendAsync()
        {
            var lblStatus = (Label)Controls.Find("lblStatus", true).FirstOrDefault();
            var username = txtAddUsername.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                lblStatus.Text = "Please enter a username.";
                return;
            }

            lblStatus.Text = "Looking up user...";

            try
            {
                var users = await new UserRepository().GetAllAsync();
                var target = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    lblStatus.Text = "User not found.";
                    return;
                }

                if (target.UserId == SessionManager.CurrentUser!.UserId)
                {
                    lblStatus.Text = "You can't add yourself.";
                    return;
                }

                var exist = _friends.FirstOrDefault(f =>
                    (f.UserId == SessionManager.CurrentUser!.UserId && f.FriendId == target.UserId) ||
                    (f.FriendId == SessionManager.CurrentUser!.UserId && f.UserId == target.UserId));

                if (exist != null && exist.Status == Models.FriendStatus.Accepted)
                {
                    lblStatus.Text = "Already friends.";
                    return;
                }

                var friend = new Friend
                {
                    UserId = SessionManager.CurrentUser!.UserId,
                    FriendId = target.UserId,
                    Status = Models.FriendStatus.Pending
                };

                var id = await _friendService.AddFriendAsync(friend);
                if (id > 0)
                {
                    lblStatus.Text = "Friend request sent!";
                    txtAddUsername.Clear();
                    _friends = (await _friendService.GetFriendsAsync(SessionManager.CurrentUser!.UserId)).ToList();
                    await FilterFriends();

                    var lblCount = (Label)Controls.Find("lblCount", true).FirstOrDefault();
                if (lblCount != null) lblCount.Text = $"{_friends.Count(f => f.Status == Models.FriendStatus.Accepted)} friends";
                }
                else
                {
                    lblStatus.Text = "Failed to send request.";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void OpenChatWithUser(int friendId)
        {
            this.Hide();
            var chat = new ChatForm(friendId);
            chat.FormClosed += (_, __) =>
            {
                _friends = new();
                _ = LoadFriendsAsync();
                this.Show();
            };
            chat.Show();
        }
    }
}