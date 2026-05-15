using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GameWikiApp.Data;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views
{
    public sealed class FriendsChatView : UserControl
    {
        private readonly FriendService _friends = new();
        private readonly ChatService _chat = new();
        private readonly UserRepository _users = new();
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly Func<int, Task> _openProfile;

        private readonly StackPanel _sidebarStack = new();
        private readonly StackPanel _searchResultsPanel = new();
        private readonly StackPanel _recentPanel = new();
        private readonly StackPanel _requestsPanel = new();
        private readonly StackPanel _friendsPanel = new();
        private readonly StackPanel _messageStack = new();
        private readonly ScrollViewer _messageScroll = new();

        private readonly TextBox _searchBox = new();
        private readonly TextBox _messageBox = new();
        private readonly TextBox _imageBox = new();
        private readonly TextBlock _headerTitle = new();
        private readonly TextBlock _headerSubtitle = new();
        private readonly TextBlock _conversationTitle = new();
        private readonly TextBlock _conversationStatus = new();
        private readonly TextBlock _composeHint = new();
        private readonly TextBlock _composeMode = new();
        private readonly TextBlock _profileCountText = new();
        private readonly Border _conversationAvatarHost = new();
        private readonly Button _sendButton = new();
        private readonly Button _attachButton = new();
        private readonly Button _clearImageButton = new();
        private readonly Button _cancelEditButton = new();

        private readonly List<Conversation> _conversations = new();
        private readonly List<Friend> _friendsList = new();
        private readonly List<Friend> _incomingRequests = new();
        private readonly List<Friend> _searchResults = new();
        private readonly List<Message> _messages = new();
        private string? _selectedAttachmentPath;

        private int? _activeConversationId;
        private int? _activeOtherUserId;
        private Conversation? _activeConversation;
        private Message? _editingMessage;
        private bool _loadedOnce;
        private bool _isDisposed;

        public FriendsChatView(Func<int, Task> openProfile)
        {
            _openProfile = openProfile;
            Content = BuildLayout();
            CancelEdit();
            UpdateComposerState();

            Loaded += async (_, __) =>
            {
                if (_loadedOnce)
                {
                    return;
                }

                _loadedOnce = true;
                await LoadAsync();
                _refreshTimer.Start();
            };

            Unloaded += (_, __) =>
            {
                _isDisposed = true;
                _refreshTimer.Stop();
            };

            _refreshTimer.Interval = TimeSpan.FromSeconds(3);
            _refreshTimer.Tick += async (_, __) => await RefreshAsync();
        }

        public async Task LoadAsync()
        {
            if (AppState.CurrentUser == null)
            {
                ShowEmptyState("No user", "Please sign in to use friends and chat.");
                return;
            }

            await ReloadSidebarAsync();

            if (!_activeConversationId.HasValue && _conversations.Count > 0)
            {
                await OpenConversationAsync(_conversations[0].ConversationId, markAsRead: true);
                return;
            }

            if (_activeConversationId.HasValue)
            {
                await LoadActiveConversationAsync(_activeConversationId.Value, markAsRead: true);
            }
            else
            {
                ShowEmptyState("Select a conversation", "Pick a friend or a recent chat from the left panel.");
            }
        }

        public async Task LoadInitialAsync()
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            await LoadAsync();
            _refreshTimer.Start();
        }

        private Control BuildLayout()
        {
            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*"),
                Background = ThemePalette.BgPrimaryBrush
            };

            root.Children.Add(BuildHeroBar());

            var body = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("370,*"),
                ColumnSpacing = 14,
                Margin = new Thickness(16, 0, 16, 16)
            };
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            var sidebar = BuildSidebar();
            body.Children.Add(sidebar);
            Grid.SetColumn(sidebar, 0);

            var conversationPane = BuildConversationPane();
            body.Children.Add(conversationPane);
            Grid.SetColumn(conversationPane, 1);

            return root;
        }

        private Control BuildHeroBar()
        {
            var shell = new Border
            {
                Margin = new Thickness(16, 16, 16, 14),
                Padding = new Thickness(22, 18),
                CornerRadius = new CornerRadius(22),
                Background = ThemePalette.SurfaceBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 14
            };

            var stack = new StackPanel
            {
                Spacing = 4
            };

            stack.Children.Add(new TextBlock
            {
                Text = "Friends & Chat",
                FontSize = 26,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextPrimaryBrush
            });

            _headerTitle.Text = "Messenger-style private chat";
            _headerTitle.FontSize = 13;
            _headerTitle.Foreground = ThemePalette.TextSecondaryBrush;
            stack.Children.Add(_headerTitle);

            _headerSubtitle.Text = "Friend requests, presence, recent chats, message editing, reads and image support.";
            _headerSubtitle.FontSize = 11;
            _headerSubtitle.Foreground = ThemePalette.TextMutedBrush;
            stack.Children.Add(_headerSubtitle);

            grid.Children.Add(stack);

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var refreshButton = UiFactory.CreateSubtleButton("Refresh", 96, "↻");
            refreshButton.Click += async (_, __) => await RefreshAsync(force: true);
            buttonRow.Children.Add(refreshButton);

            var newChatButton = UiFactory.CreatePrimaryButton("Open latest", 116, "✉");
            newChatButton.Click += async (_, __) =>
            {
                if (_conversations.Count > 0)
                {
                    await OpenConversationAsync(_conversations[0].ConversationId, markAsRead: true);
                }
            };
            buttonRow.Children.Add(newChatButton);

            grid.Children.Add(buttonRow);
            Grid.SetColumn(buttonRow, 1);

            shell.Child = grid;
            return shell;
        }

        private Control BuildSidebar()
        {
            var shell = new Border
            {
                Background = ThemePalette.BgSecondaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(22),
                Padding = new Thickness(0),
                ClipToBounds = true
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _sidebarStack,
                Background = Brushes.Transparent
            };

            _sidebarStack.Spacing = 14;
            _sidebarStack.Margin = new Thickness(14);
            _sidebarStack.Children.Add(BuildProfileCard());
            _sidebarStack.Children.Add(BuildSearchCard());
            _sidebarStack.Children.Add(_searchResultsPanel);
            _sidebarStack.Children.Add(_recentPanel);
            _sidebarStack.Children.Add(_requestsPanel);
            _sidebarStack.Children.Add(_friendsPanel);

            shell.Child = scroll;
            return shell;
        }

        private Control BuildProfileCard()
        {
            var shell = UiFactory.CreateCard(null, null, 0);
            shell.Background = ThemePalette.BgCardBrush;
            shell.BorderBrush = ThemePalette.BorderLightBrush;
            shell.CornerRadius = new CornerRadius(18);
            shell.Padding = new Thickness(16);
            shell.Cursor = new Cursor(StandardCursorType.Hand);
            shell.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                if (AppState.CurrentUser != null)
                {
                    await OpenProfileAsync(AppState.CurrentUser.UserId);
                }
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("56,*,Auto"),
                ColumnSpacing = 12
            };

            var avatar = CreateAvatarShell(AppState.CurrentUser?.ProfileImage, AppState.CurrentUser?.Username ?? "U", 48, AppState.CurrentUser?.IsOnline ?? false);
            grid.Children.Add(avatar);

            var info = new StackPanel
            {
                Spacing = 3,
                VerticalAlignment = VerticalAlignment.Center
            };

            info.Children.Add(new TextBlock
            {
                Text = AppState.CurrentUser?.Username ?? "Guest",
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextPrimaryBrush
            });

            info.Children.Add(new TextBlock
            {
                Text = AppState.CurrentUser?.IsOnline == true ? "Online now" : "Offline",
                FontSize = 11,
                Foreground = AppState.CurrentUser?.IsOnline == true ? ThemePalette.SuccessBrush : ThemePalette.TextMutedBrush
            });

            grid.Children.Add(info);
            Grid.SetColumn(info, 1);

            _profileCountText.Text = $"{_conversations.Count} chats";
            _profileCountText.FontSize = 10;
            _profileCountText.Foreground = ThemePalette.TextMutedBrush;
            _profileCountText.VerticalAlignment = VerticalAlignment.Bottom;
            _profileCountText.HorizontalAlignment = HorizontalAlignment.Right;
            grid.Children.Add(_profileCountText);
            Grid.SetColumn(_profileCountText, 2);

            shell.Child = grid;
            return shell;
        }

        private Control BuildSearchCard()
        {
            var shell = UiFactory.CreateCard(null, null, 0);
            shell.Background = ThemePalette.BgCardBrush;
            shell.BorderBrush = ThemePalette.BorderLightBrush;
            shell.CornerRadius = new CornerRadius(18);
            shell.Padding = new Thickness(16);

            var stack = new StackPanel
            {
                Spacing = 10
            };

            stack.Children.Add(new TextBlock
            {
                Text = "Find people",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextPrimaryBrush
            });

            _searchBox.PlaceholderText = "Search by username or email";
            _searchBox.Background = ThemePalette.BgInputBrush;
            _searchBox.BorderBrush = ThemePalette.BorderBrush;
            _searchBox.Foreground = ThemePalette.TextPrimaryBrush;
            _searchBox.CaretBrush = ThemePalette.AccentBrush;
            _searchBox.CornerRadius = new CornerRadius(12);
            _searchBox.Padding = new Thickness(12, 10);
            _searchBox.Height = 42;
            _searchBox.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    await SearchUsersAsync();
                }
            };
            stack.Children.Add(_searchBox);

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var searchButton = UiFactory.CreatePrimaryButton("Search", 92, "⌕");
            searchButton.Click += async (_, __) => await SearchUsersAsync();
            buttonRow.Children.Add(searchButton);

            var clearButton = UiFactory.CreateSubtleButton("Clear", 82, "×");
            clearButton.Click += async (_, __) =>
            {
                _searchBox.Text = string.Empty;
                _searchResults.Clear();
                await ReloadSidebarAsync();
            };
            buttonRow.Children.Add(clearButton);

            stack.Children.Add(buttonRow);

            var hint = new TextBlock
            {
                Text = "Send a request directly from the search results.",
                FontSize = 11,
                Foreground = ThemePalette.TextMutedBrush,
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(hint);

            shell.Child = stack;
            return shell;
        }

        private Control BuildConversationPane()
        {
            var shell = new Border
            {
                Background = ThemePalette.BgSecondaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(22),
                ClipToBounds = true
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Background = Brushes.Transparent
            };

            grid.Children.Add(BuildConversationHeader());

            _messageScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _messageScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _messageScroll.Content = _messageStack;
            _messageScroll.Background = ThemePalette.BgPrimaryBrush;

            _messageStack.Spacing = 12;
            _messageStack.Margin = new Thickness(16);

            grid.Children.Add(_messageScroll);
            Grid.SetRow(_messageScroll, 1);

            var composer = BuildComposer();
            grid.Children.Add(composer);
            Grid.SetRow(composer, 2);

            shell.Child = grid;
            return shell;
        }

        private Control BuildConversationHeader()
        {
            var shell = new Border
            {
                Background = ThemePalette.BgSecondaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 16),
                Margin = new Thickness(0)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                ColumnSpacing = 12
            };

            _conversationAvatarHost.Child = CreateAvatarShell(null, "?", 46, false);
            _conversationAvatarHost.Cursor = new Cursor(StandardCursorType.Hand);
            _conversationAvatarHost.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                if (_activeOtherUserId.HasValue)
                {
                    await OpenProfileAsync(_activeOtherUserId.Value);
                }
            };
            grid.Children.Add(_conversationAvatarHost);

            var stack = new StackPanel
            {
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Center
            };

            _conversationTitle.Text = "Select a conversation";
            _conversationTitle.FontSize = 16;
            _conversationTitle.FontWeight = FontWeight.Bold;
            _conversationTitle.Foreground = ThemePalette.TextPrimaryBrush;
            _conversationTitle.Cursor = new Cursor(StandardCursorType.Hand);
            _conversationTitle.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                if (_activeOtherUserId.HasValue)
                {
                    await OpenProfileAsync(_activeOtherUserId.Value);
                }
            };
            stack.Children.Add(_conversationTitle);

            _conversationStatus.Text = "Messages appear here once you choose a friend or recent chat.";
            _conversationStatus.FontSize = 11;
            _conversationStatus.Foreground = ThemePalette.TextMutedBrush;
            stack.Children.Add(_conversationStatus);

            grid.Children.Add(stack);
            Grid.SetColumn(stack, 1);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center
            };

            var openFriendButton = UiFactory.CreateSubtleButton("Open friend", 104, "✉");
            openFriendButton.Click += async (_, __) =>
            {
                if (_activeOtherUserId.HasValue)
                {
                    await OpenConversationByUserAsync(_activeOtherUserId.Value);
                }
            };
            actions.Children.Add(openFriendButton);

            grid.Children.Add(actions);
            Grid.SetColumn(actions, 2);

            shell.Child = grid;
            return shell;
        }

        private Control BuildComposer()
        {
            var shell = new Border
            {
                Background = ThemePalette.BgSecondaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1, 1, 0, 0),
                Padding = new Thickness(16),
                Margin = new Thickness(0)
            };

            var stack = new StackPanel
            {
                Spacing = 10
            };

            _composeMode.Text = "Press Enter to send. Shift+Enter adds a new line.";
            _composeMode.FontSize = 11;
            _composeMode.Foreground = ThemePalette.TextMutedBrush;
            stack.Children.Add(_composeMode);

            var inputCard = new Border
            {
                Background = ThemePalette.BgCardBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12)
            };

            _messageBox.AcceptsReturn = true;
            _messageBox.TextWrapping = TextWrapping.Wrap;
            _messageBox.MinHeight = 72;
            _messageBox.MaxHeight = 140;
            _messageBox.Background = Brushes.Transparent;
            _messageBox.BorderBrush = Brushes.Transparent;
            _messageBox.CaretBrush = ThemePalette.AccentBrush;
            _messageBox.Foreground = ThemePalette.TextPrimaryBrush;
            _messageBox.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    e.Handled = true;
                    await SendCurrentMessageAsync();
                }
            };

            inputCard.Child = _messageBox;
            stack.Children.Add(inputCard);

            var attachmentRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 10
            };

            _imageBox.PlaceholderText = "Image URL or local path (optional)";
            _imageBox.Background = ThemePalette.BgCardBrush;
            _imageBox.BorderBrush = ThemePalette.BorderLightBrush;
            _imageBox.Foreground = ThemePalette.TextPrimaryBrush;
            _imageBox.CaretBrush = ThemePalette.AccentBrush;
            _imageBox.CornerRadius = new CornerRadius(12);
            _imageBox.Padding = new Thickness(12, 10);
            _imageBox.Height = 42;
            attachmentRow.Children.Add(_imageBox);

            _attachButton.Content = UiFactory.CreateButtonContent("Browse", "↑");
            _attachButton.Background = ThemePalette.BgTertiaryBrush;
            _attachButton.BorderBrush = ThemePalette.BorderLightBrush;
            _attachButton.BorderThickness = new Thickness(1);
            _attachButton.Foreground = ThemePalette.TextPrimaryBrush;
            _attachButton.CornerRadius = new CornerRadius(10);
            _attachButton.Padding = new Thickness(14, 10);
            _attachButton.MinWidth = 90;
            _attachButton.Click += async (_, __) => await PickImageAsync();
            attachmentRow.Children.Add(_attachButton);
            Grid.SetColumn(_attachButton, 1);

            _clearImageButton.Content = UiFactory.CreateButtonContent("Clear", "×");
            _clearImageButton.Background = ThemePalette.BgTertiaryBrush;
            _clearImageButton.BorderBrush = ThemePalette.BorderLightBrush;
            _clearImageButton.BorderThickness = new Thickness(1);
            _clearImageButton.Foreground = ThemePalette.TextPrimaryBrush;
            _clearImageButton.CornerRadius = new CornerRadius(10);
            _clearImageButton.Padding = new Thickness(14, 10);
            _clearImageButton.MinWidth = 84;
            _clearImageButton.Click += (_, __) =>
            {
                _imageBox.Text = string.Empty;
                _selectedAttachmentPath = null;
            };
            attachmentRow.Children.Add(_clearImageButton);
            Grid.SetColumn(_clearImageButton, 2);

            stack.Children.Add(attachmentRow);

            var footerRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 10
            };

            _composeHint.Text = string.Empty;
            _composeHint.FontSize = 11;
            _composeHint.Foreground = ThemePalette.TextMutedBrush;
            footerRow.Children.Add(_composeHint);

            _cancelEditButton.Content = UiFactory.CreateButtonContent("Cancel edit", "↩");
            _cancelEditButton.Background = ThemePalette.BgTertiaryBrush;
            _cancelEditButton.BorderBrush = ThemePalette.BorderLightBrush;
            _cancelEditButton.BorderThickness = new Thickness(1);
            _cancelEditButton.Foreground = ThemePalette.TextPrimaryBrush;
            _cancelEditButton.CornerRadius = new CornerRadius(10);
            _cancelEditButton.Padding = new Thickness(14, 10);
            _cancelEditButton.MinWidth = 104;
            _cancelEditButton.Click += (_, __) => CancelEdit();
            footerRow.Children.Add(_cancelEditButton);
            Grid.SetColumn(_cancelEditButton, 1);

            _sendButton.Content = UiFactory.CreateButtonContent("Send", "➤");
            _sendButton.Background = ThemePalette.AccentBrush;
            _sendButton.BorderBrush = Brushes.Transparent;
            _sendButton.Foreground = ThemePalette.AccentForegroundBrush;
            _sendButton.CornerRadius = new CornerRadius(10);
            _sendButton.Padding = new Thickness(18, 10);
            _sendButton.MinWidth = 110;
            _sendButton.Click += async (_, __) => await SendCurrentMessageAsync();
            footerRow.Children.Add(_sendButton);
            Grid.SetColumn(_sendButton, 2);

            stack.Children.Add(footerRow);

            shell.Child = stack;
            return shell;
        }

        private async Task ReloadSidebarAsync()
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var userId = AppState.CurrentUser.UserId;
            var friendsTask = _friends.GetFriendsAsync(userId);
            var requestsTask = _friends.GetIncomingRequestsAsync(userId);
            var recentTask = _chat.GetRecentConversationsAsync(userId);
            var searchTask = string.IsNullOrWhiteSpace(_searchBox.Text)
                ? Task.FromResult<IEnumerable<Friend>>(Array.Empty<Friend>())
                : _friends.SearchUsersAsync(userId, _searchBox.Text.Trim());

            await Task.WhenAll(friendsTask, requestsTask, recentTask, searchTask);

            _friendsList.Clear();
            _friendsList.AddRange(await friendsTask);

            _incomingRequests.Clear();
            _incomingRequests.AddRange(await requestsTask);

            _conversations.Clear();
            _conversations.AddRange(await recentTask);

            _searchResults.Clear();
            _searchResults.AddRange(await searchTask);

            RefreshSearchResultsPanel();
            RefreshRecentPanel();
            RefreshRequestsPanel();
            RefreshFriendsPanel();
            UpdateHeaderSummary();
            _profileCountText.Text = $"{_conversations.Count} chats";
        }

        private async Task RefreshAsync(bool force = false)
        {
            if (_isDisposed)
            {
                return;
            }

            if (!_refreshLock.Wait(0))
            {
                return;
            }

            try
            {
                if (AppState.CurrentUser == null)
                {
                    return;
                }

                await ReloadSidebarAsync();

                if (_activeConversationId.HasValue)
                {
                    await LoadActiveConversationAsync(_activeConversationId.Value, markAsRead: true);
                }
                else if (force && _conversations.Count > 0)
                {
                    await OpenConversationAsync(_conversations[0].ConversationId, markAsRead: true);
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private void UpdateSidebarSummary()
        {
            _headerTitle.Text = $"{_friendsList.Count} friends · {_incomingRequests.Count} pending requests · {_conversations.Count} chats";
            _headerTitle.Text = $"{_friendsList.Count} friends · {_incomingRequests.Count} pending requests · {_conversations.Count} chats";
            _headerSubtitle.Text = string.IsNullOrWhiteSpace(_searchBox.Text)
                ? "Use the search field to find people, then open a chat or send a request."
                : $"Showing search results for \"{_searchBox.Text.Trim()}\".";
        }

        private void RefreshSearchResultsPanel()
        {
            _searchResultsPanel.Children.Clear();
            _searchResultsPanel.Children.Add(BuildSectionHeader("Search results", $"{_searchResults.Count} people"));

            if (_searchResults.Count == 0)
            {
                _searchResultsPanel.Children.Add(BuildEmptyCard("No matches", "Search by username or email to send a request."));
                return;
            }

            foreach (var friend in _searchResults.Take(8))
            {
                _searchResultsPanel.Children.Add(CreateSearchResultRow(friend));
            }
        }

        private void RefreshRecentPanel()
        {
            _recentPanel.Children.Clear();
            _recentPanel.Children.Add(BuildSectionHeader("Recent chats", $"{_conversations.Count} conversations"));

            if (_conversations.Count == 0)
            {
                _recentPanel.Children.Add(BuildEmptyCard("No conversations yet", "Open a friend to start your first private chat."));
                return;
            }

            foreach (var conversation in _conversations.Take(8))
            {
                _recentPanel.Children.Add(CreateConversationRow(conversation));
            }
        }

        private void RefreshRequestsPanel()
        {
            _requestsPanel.Children.Clear();
            _requestsPanel.Children.Add(BuildSectionHeader("Friend requests", $"{_incomingRequests.Count} pending"));

            if (_incomingRequests.Count == 0)
            {
                _requestsPanel.Children.Add(BuildEmptyCard("No requests", "Incoming friend requests will appear here."));
                return;
            }

            foreach (var request in _incomingRequests.Take(6))
            {
                _requestsPanel.Children.Add(CreateRequestRow(request));
            }
        }

        private void RefreshFriendsPanel()
        {
            _friendsPanel.Children.Clear();
            _friendsPanel.Children.Add(BuildSectionHeader("Friends", $"{_friendsList.Count} online + offline"));

            if (_friendsList.Count == 0)
            {
                _friendsPanel.Children.Add(BuildEmptyCard("No friends yet", "Accept a request or search for someone to connect."));
                return;
            }

            foreach (var friend in _friendsList.Take(10))
            {
                _friendsPanel.Children.Add(CreateFriendRow(friend));
            }
        }

        private async Task SearchUsersAsync()
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var query = _searchBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
            {
                _searchResults.Clear();
                RefreshSearchResultsPanel();
                UpdateHeaderSummary();
                return;
            }

            _searchResults.Clear();
            var results = await _friends.SearchUsersAsync(AppState.CurrentUser.UserId, query, 12);
            _searchResults.AddRange(results);

            RefreshSearchResultsPanel();
            UpdateHeaderSummary();
        }

        public async Task OpenConversationByUserAsync(int otherUserId)
        {
            var current = AppState.CurrentUser;
            if (current == null)
            {
                return;
            }

            var result = await _chat.OpenPrivateConversationAsync(current.UserId, otherUserId);
            if (result.conversation == null)
            {
                ShowEmptyState("Conversation unavailable", result.error ?? "The chat could not be opened.");
                return;
            }

            await OpenConversationAsync(result.conversation.ConversationId, markAsRead: true);
            await ReloadSidebarAsync();
        }

        public async Task OpenConversationAsync(int conversationId, bool markAsRead)
        {
            _activeConversationId = conversationId;
            await LoadActiveConversationAsync(conversationId, markAsRead);
        }

        private async Task LoadActiveConversationAsync(int conversationId, bool markAsRead)
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var conversation = await _chat.GetConversationAsync(conversationId, AppState.CurrentUser.UserId, markAsRead);
            if (conversation == null)
            {
                ShowEmptyState("Conversation not found", "The selected chat is no longer available.");
                return;
            }

            _activeConversation = conversation;
            _activeOtherUserId = conversation.OtherUserId;
            UpdateHeaderSummaryForConversation(conversation);
            _conversationAvatarHost.Child = MakeProfileAvatar(conversation.OtherProfileImage, conversation.OtherUsername, 46, conversation.OtherIsOnline, conversation.OtherUserId);

            var messages = (await _chat.GetMessagesAsync(conversationId, AppState.CurrentUser.UserId, false)).ToList();
            _messages.Clear();
            _messages.AddRange(messages);

            await ReloadMessagesPanelAsync();
            UpdateComposerState();
            HighlightSelection();
            ScrollMessagesToBottom();
        }

        private async Task ReloadMessagesPanelAsync()
        {
            _messageStack.Children.Clear();

            if (_messages.Count == 0)
            {
                _messageStack.Children.Add(BuildEmptyCard("No messages", "Start the conversation with a friendly hello."));
                return;
            }

            for (var i = 0; i < _messages.Count; i++)
            {
                var message = _messages[i];
                _messageStack.Children.Add(await CreateMessageBubbleAsync(message));
            }
        }

        private Control BuildSectionHeader(string title, string caption)
        {
            var shell = UiFactory.CreateSectionHeader(title, caption);
            shell.Margin = new Thickness(0, 0, 0, 0);
            return shell;
        }

        private Control BuildEmptyCard(string title, string message)
        {
            return new Border
            {
                Background = ThemePalette.BgCardBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 13,
                            FontWeight = FontWeight.Bold,
                            Foreground = ThemePalette.TextPrimaryBrush
                        },
                        new TextBlock
                        {
                            Text = message,
                            FontSize = 11,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = ThemePalette.TextMutedBrush
                        }
                    }
                }
            };
        }

        private Control CreateConversationRow(Conversation conversation)
        {
            var selected = _activeConversationId == conversation.ConversationId;
            var shell = new Border
            {
                Background = selected ? ThemePalette.BgTertiaryBrush : ThemePalette.BgCardBrush,
                BorderBrush = selected ? ThemePalette.AccentBrush : ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("48,*,Auto"),
                ColumnSpacing = 12
            };

            grid.Children.Add(MakeProfileAvatar(conversation.OtherProfileImage, conversation.OtherUsername, 40, conversation.OtherIsOnline, conversation.OtherUserId));

            var stack = new StackPanel
            {
                Spacing = 3
            };

            if (conversation.IsGroupChat)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = conversation.DisplayName,
                    FontSize = 13.5,
                    FontWeight = FontWeight.Bold,
                    Foreground = ThemePalette.TextPrimaryBrush,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }
            else
            {
                stack.Children.Add(MakeProfileText(conversation.DisplayName, conversation.OtherUserId));
            }

            var preview = conversation.PreviewText;
            if (AppState.CurrentUser != null && conversation.LastMessageSenderId == AppState.CurrentUser.UserId)
            {
                preview = "You: " + preview;
            }

            if (!string.IsNullOrWhiteSpace(preview) && preview.Length > 66)
            {
                preview = preview[..66] + "...";
            }

            stack.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(preview) ? "No messages yet" : preview,
                FontSize = 11,
                Foreground = ThemePalette.TextMutedBrush,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            grid.Children.Add(stack);
            Grid.SetColumn(stack, 1);

            var meta = new StackPanel
            {
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            meta.Children.Add(new TextBlock
            {
                Text = conversation.LastMessageAt.HasValue ? FormatShortTime(conversation.LastMessageAt.Value) : FormatShortTime(conversation.CreatedAt),
                FontSize = 10,
                Foreground = ThemePalette.TextMutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            if (conversation.HasUnread)
            {
                meta.Children.Add(new Border
                {
                    Background = ThemePalette.AccentBrush,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 2),
                    Child = new TextBlock
                    {
                        Text = conversation.UnreadCount > 99 ? "99+" : conversation.UnreadCount.ToString(),
                        FontSize = 10,
                        FontWeight = FontWeight.Bold,
                        Foreground = ThemePalette.AccentForegroundBrush
                    }
                });
            }

            grid.Children.Add(meta);
            Grid.SetColumn(meta, 2);

            shell.Child = grid;
            shell.PointerEntered += (_, __) =>
            {
                if (!selected)
                {
                    _ = UiAnimation.BackgroundColorToAsync(shell, ThemePalette.BgTertiary, 120);
                }
            };
            shell.PointerExited += (_, __) =>
            {
                if (!selected)
                {
                    _ = UiAnimation.BackgroundColorToAsync(shell, ThemePalette.BgCard, 120);
                }
            };
            shell.PointerPressed += async (_, __) => await OpenConversationAsync(conversation.ConversationId, markAsRead: true);
            return shell;
        }

        private Control CreateFriendRow(Friend friend)
        {
            var shell = new Border
            {
                Background = ThemePalette.BgCardBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("48,*,Auto"),
                ColumnSpacing = 12
            };

            grid.Children.Add(MakeProfileAvatar(friend.OtherProfileImage, friend.OtherUsername, 40, friend.OtherIsOnline, friend.OtherUserId));

            var stack = new StackPanel
            {
                Spacing = 3
            };

            stack.Children.Add(MakeProfileText(friend.OtherUsername, friend.OtherUserId));

            stack.Children.Add(new TextBlock
            {
                Text = BuildPresenceText(friend.OtherIsOnline, friend.OtherLastSeen),
                FontSize = 11,
                Foreground = friend.OtherIsOnline ? ThemePalette.SuccessBrush : ThemePalette.TextMutedBrush
            });

            grid.Children.Add(stack);
            Grid.SetColumn(stack, 1);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };

            var chatButton = UiFactory.CreateSubtleButton("Chat", 72);
            chatButton.Click += async (_, __) => await OpenConversationByUserAsync(friend.OtherUserId);
            actions.Children.Add(chatButton);

            var removeButton = UiFactory.CreateSubtleButton("Remove", 82);
            removeButton.Click += async (_, __) => await RemoveFriendAsync(friend.OtherUserId);
            actions.Children.Add(removeButton);

            grid.Children.Add(actions);
            Grid.SetColumn(actions, 2);

            shell.Child = grid;
            return shell;
        }

        private Control CreateRequestRow(Friend friend)
        {
            var shell = new Border
            {
                Background = ThemePalette.BgCardBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("48,*,Auto"),
                ColumnSpacing = 12
            };

            grid.Children.Add(MakeProfileAvatar(friend.OtherProfileImage, friend.OtherUsername, 40, friend.OtherIsOnline, friend.OtherUserId));

            var stack = new StackPanel
            {
                Spacing = 3
            };

            stack.Children.Add(MakeProfileText(friend.OtherUsername, friend.OtherUserId));

            stack.Children.Add(new TextBlock
            {
                Text = "Sent you a friend request",
                FontSize = 11,
                Foreground = ThemePalette.TextMutedBrush
            });

            grid.Children.Add(stack);
            Grid.SetColumn(stack, 1);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };

            var accept = UiFactory.CreatePrimaryButton("Accept", 82);
            accept.Click += async (_, __) => await AcceptRequestAsync(friend);
            actions.Children.Add(accept);

            var decline = UiFactory.CreateSubtleButton("Decline", 82);
            decline.Click += async (_, __) => await DeclineRequestAsync(friend);
            actions.Children.Add(decline);

            grid.Children.Add(actions);
            Grid.SetColumn(actions, 2);

            shell.Child = grid;
            return shell;
        }

        private Control CreateSearchResultRow(Friend friend)
        {
            var shell = new Border
            {
                Background = ThemePalette.BgCardBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("48,*,Auto"),
                ColumnSpacing = 12
            };

            grid.Children.Add(MakeProfileAvatar(friend.OtherProfileImage, friend.OtherUsername, 40, friend.OtherIsOnline, friend.OtherUserId));

            var stack = new StackPanel
            {
                Spacing = 3
            };

            stack.Children.Add(MakeProfileText(friend.OtherUsername, friend.OtherUserId));

            stack.Children.Add(new TextBlock
            {
                Text = friend.Status switch
                {
                    "accepted" => "Already friends",
                    "pending" when friend.IsIncomingRequest => "Request pending from this user",
                    "pending" => "Friend request pending",
                    "blocked" => "Blocked",
                    _ => BuildPresenceText(friend.OtherIsOnline, friend.OtherLastSeen)
                },
                FontSize = 11,
                Foreground = ThemePalette.TextMutedBrush
            });

            grid.Children.Add(stack);
            Grid.SetColumn(stack, 1);

            var action = new Button
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10),
                MinWidth = 110,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            switch (friend.Status)
            {
                case "accepted":
                    action.Content = "Open chat";
                    action.Background = ThemePalette.AccentBrush;
                    action.Foreground = ThemePalette.AccentForegroundBrush;
                    action.BorderBrush = Brushes.Transparent;
                    action.Click += async (_, __) => await OpenConversationByUserAsync(friend.OtherUserId);
                    break;
                case "pending" when friend.IsIncomingRequest:
                    action.Content = "Accept";
                    action.Background = ThemePalette.AccentBrush;
                    action.Foreground = ThemePalette.AccentForegroundBrush;
                    action.BorderBrush = Brushes.Transparent;
                    action.Click += async (_, __) => await AcceptRequestFromSearchAsync(friend);
                    break;
                case "pending":
                    action.Content = "Pending";
                    action.Background = ThemePalette.BgTertiaryBrush;
                    action.Foreground = ThemePalette.TextSecondaryBrush;
                    action.BorderBrush = ThemePalette.BorderLightBrush;
                    action.Click += (_, __) => { };
                    break;
                case "blocked":
                    action.Content = "Blocked";
                    action.Background = ThemePalette.BgTertiaryBrush;
                    action.Foreground = ThemePalette.TextMutedBrush;
                    action.BorderBrush = ThemePalette.BorderLightBrush;
                    action.Click += (_, __) => { };
                    break;
                default:
                    action.Content = "Add friend";
                    action.Background = ThemePalette.AccentBrush;
                    action.Foreground = ThemePalette.AccentForegroundBrush;
                    action.BorderBrush = Brushes.Transparent;
                    action.Click += async (_, __) => await SendRequestAsync(friend.OtherUserId);
                    break;
            }

            grid.Children.Add(action);
            Grid.SetColumn(action, 2);

            shell.Child = grid;
            return shell;
        }

        private async Task AcceptRequestAsync(Friend request)
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var result = await _friends.AcceptRequestAsync(AppState.CurrentUser.UserId, request.FriendshipId);
            if (result.success)
            {
                await ReloadAfterSocialChangeAsync(request.OtherUserId);
            }
        }

        private async Task DeclineRequestAsync(Friend request)
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var result = await _friends.DeclineRequestAsync(AppState.CurrentUser.UserId, request.FriendshipId);
            if (result.success)
            {
                await ReloadAfterSocialChangeAsync(request.OtherUserId);
            }
        }

        private async Task AcceptRequestFromSearchAsync(Friend friend)
        {
            await AcceptRequestAsync(friend);
        }

        private async Task SendRequestAsync(int otherUserId)
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var result = await _friends.SendRequestAsync(AppState.CurrentUser.UserId, otherUserId);
            if (result.success)
            {
                await ReloadSidebarAsync();
                RefreshSearchResultsPanel();
            }
        }

        private async Task RemoveFriendAsync(int otherUserId)
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var result = await _friends.RemoveFriendAsync(AppState.CurrentUser.UserId, otherUserId);
            if (result.success)
            {
                await ReloadAfterSocialChangeAsync(otherUserId);
            }
        }

        private async Task ReloadAfterSocialChangeAsync(int otherUserId)
        {
            await ReloadSidebarAsync();

            if (_activeOtherUserId == otherUserId)
            {
                await OpenConversationByUserAsync(otherUserId);
            }
            else if (_activeConversationId.HasValue)
            {
                await LoadActiveConversationAsync(_activeConversationId.Value, markAsRead: true);
            }
        }

        private async Task SendCurrentMessageAsync()
        {
            if (AppState.CurrentUser == null || !_activeConversationId.HasValue)
            {
                return;
            }

            var text = _messageBox.Text?.Trim() ?? string.Empty;
            var image = _imageBox.Text?.Trim();

            if (_editingMessage != null)
            {
                var edit = await _chat.EditMessageAsync(_editingMessage.MessageId, AppState.CurrentUser.UserId, text, image);
                if (!edit.success)
                {
                    return;
                }
            }
            else
            {
                var send = await _chat.SendMessageAsync(_activeConversationId.Value, AppState.CurrentUser.UserId, text, image);
                if (!send.success)
                {
                    return;
                }
            }

            _messageBox.Text = string.Empty;
            _imageBox.Text = string.Empty;
            CancelEdit();
            await LoadActiveConversationAsync(_activeConversationId.Value, markAsRead: true);
            await ReloadSidebarAsync();
        }

        private void CancelEdit()
        {
            _editingMessage = null;
            _composeMode.Text = "Press Enter to send. Shift+Enter adds a new line.";
            _sendButton.Content = UiFactory.CreateButtonContent("Send", "➤");
            _cancelEditButton.IsVisible = false;
            _messageBox.Text = string.Empty;
            _imageBox.Text = string.Empty;
        }

        private void UpdateComposerState()
        {
            _sendButton.Content = UiFactory.CreateButtonContent(_editingMessage == null ? "Send" : "Update", _editingMessage == null ? "➤" : "✓");
            _cancelEditButton.IsVisible = _editingMessage != null;
            _composeMode.Text = _editingMessage == null
                ? "Press Enter to send. Shift+Enter adds a new line."
                : "Editing message. Press Update to save the changes.";

            if (_activeConversationId.HasValue)
            {
                _messageBox.IsEnabled = true;
                _imageBox.IsEnabled = true;
                _sendButton.IsEnabled = true;
                _attachButton.IsEnabled = true;
                _clearImageButton.IsEnabled = true;
                _cancelEditButton.IsEnabled = true;
            }
            else
            {
                _messageBox.IsEnabled = false;
                _imageBox.IsEnabled = false;
                _sendButton.IsEnabled = false;
                _attachButton.IsEnabled = false;
                _clearImageButton.IsEnabled = false;
                _cancelEditButton.IsEnabled = false;
            }
        }

        private void ShowEmptyState(string title, string message)
        {
            _conversationTitle.Text = title;
            _conversationStatus.Text = message;
            _conversationAvatarHost.Child = CreateAvatarShell(null, "?", 46, false);

            _messageStack.Children.Clear();
            _messageStack.Children.Add(BuildEmptyCard(title, message));
            _activeConversation = null;
            _activeConversationId = null;
            _activeOtherUserId = null;
            CancelEdit();
            UpdateComposerState();
        }

        private void HighlightSelection()
        {
            // Sidebar is rebuilt on refresh, so selection is applied by row creation.
        }

        private void ScrollMessagesToBottom()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _messageScroll.Offset = new Vector(0, 1_000_000);
                }
                catch
                {
                    // ignore scroll issues
                }
            }, DispatcherPriority.Background);
        }

        private async Task<Control> CreateMessageBubbleAsync(Message message)
        {
            var outer = new StackPanel
            {
                Spacing = 6,
                HorizontalAlignment = message.IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 2)
            };

            if (!message.IsMine)
            {
                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("40,*"),
                    ColumnSpacing = 10
                };

                row.Children.Add(MakeProfileAvatar(message.SenderProfileImage, message.SenderUsername, 34, false, message.SenderId));

                var bubble = await BuildBubbleAsync(message, false);
                row.Children.Add(bubble);
                Grid.SetColumn(bubble, 1);
                outer.Children.Add(row);
                return outer;
            }

            outer.Children.Add(await BuildBubbleAsync(message, true));
            return outer;
        }

        private async Task<Control> BuildBubbleAsync(Message message, bool mine)
        {
            var shell = new Border
            {
                Background = mine ? ThemePalette.AccentBrush : ThemePalette.BgCardBrush,
                BorderBrush = mine ? Brushes.Transparent : ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(mine ? 0 : 1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14),
                MaxWidth = 560,
                HorizontalAlignment = mine ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var stack = new StackPanel
            {
                Spacing = 8
            };

            if (message.HasImage)
            {
                var bitmap = await ImageLoader.LoadAsync(message.ImageUrl);
                if (bitmap != null)
                {
                    stack.Children.Add(new Border
                    {
                        Width = 260,
                        Height = 170,
                        CornerRadius = new CornerRadius(14),
                        ClipToBounds = true,
                        Child = new Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }
                else
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = message.ImageUrl ?? string.Empty,
                        FontSize = 11,
                        Foreground = mine ? ThemePalette.AccentForegroundBrush : ThemePalette.AccentBrush,
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }

            if (message.HasText || message.IsDeleted)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = message.Body,
                    FontSize = 13,
                    Foreground = mine ? ThemePalette.AccentForegroundBrush : ThemePalette.TextPrimaryBrush,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            footer.Children.Add(new TextBlock
            {
                Text = FormatShortTime(message.SentAt),
                FontSize = 10,
                Foreground = mine ? ThemePalette.AccentForegroundBrush : ThemePalette.TextMutedBrush
            });

            if (message.IsEdited)
            {
                footer.Children.Add(new TextBlock
                {
                    Text = "Edited",
                    FontSize = 10,
                    Foreground = mine ? ThemePalette.AccentForegroundBrush : ThemePalette.TextMutedBrush
                });
            }

            if (mine)
            {
                footer.Children.Add(new TextBlock
                {
                    Text = message.IsDeleted ? string.Empty : (message.HasReadReceipt ? "Seen" : "Sent"),
                    FontSize = 10,
                    Foreground = ThemePalette.AccentForegroundBrush,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
            }

            stack.Children.Add(footer);

            if (mine && !message.IsDeleted)
            {
                var actionRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var editButton = UiFactory.CreateSubtleButton("Edit", 68);
                editButton.Click += (_, __) => BeginEdit(message);
                actionRow.Children.Add(editButton);

                var deleteButton = UiFactory.CreateSubtleButton("Delete", 74);
                deleteButton.Click += async (_, __) => await DeleteMessageAsync(message);
                actionRow.Children.Add(deleteButton);

                stack.Children.Add(actionRow);
            }

            shell.Child = stack;
            return shell;
        }

        private void BeginEdit(Message message)
        {
            _editingMessage = message;
            _messageBox.Text = message.MessageText ?? string.Empty;
            _imageBox.Text = message.ImageUrl ?? string.Empty;
            _composeMode.Text = $"Editing message from {FormatShortTime(message.SentAt)}.";
            _sendButton.Content = UiFactory.CreateButtonContent("Update", "✓");
            _cancelEditButton.IsVisible = true;
        }

        private async Task DeleteMessageAsync(Message message)
        {
            if (AppState.CurrentUser == null)
            {
                return;
            }

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner != null && !await DialogHelper.ConfirmAsync(owner, "Delete message", "Delete this message?"))
            {
                return;
            }

            var result = await _chat.DeleteMessageAsync(message.MessageId, AppState.CurrentUser.UserId);
            if (result.success && _activeConversationId.HasValue)
            {
                await LoadActiveConversationAsync(_activeConversationId.Value, markAsRead: true);
                await ReloadSidebarAsync();
            }
        }

        private async Task PickImageAsync()
        {
            var top = TopLevel.GetTopLevel(this);
            if (top?.StorageProvider == null)
            {
                return;
            }

            var imageType = new FilePickerFileType("Images")
            {
                Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" }
            };

            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select chat image",
                AllowMultiple = false,
                FileTypeFilter = new[] { imageType }
            });

            if (files.Count == 0)
            {
                return;
            }

            var localPath = files[0].TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            var validation = await ImageValidation.ValidateLocalImageAsync(localPath);
            if (!validation.Success)
            {
                return;
            }

            var stored = await CopyImageToAppFolderAsync(localPath);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return;
            }

            _imageBox.Text = stored;
            _selectedAttachmentPath = stored;
        }

        private static async Task<string?> CopyImageToAppFolderAsync(string localPath)
        {
            try
            {
                if (!File.Exists(localPath))
                {
                    return null;
                }

                var validation = await ImageValidation.ValidateLocalImageAsync(localPath);
                if (!validation.Success)
                {
                    return null;
                }

                var extension = Path.GetExtension(localPath);
                var folder = Path.Combine(AppContext.BaseDirectory, "Photo", "Chats");
                Directory.CreateDirectory(folder);

                var fileName = $"chat_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
                var destination = Path.Combine(folder, fileName);

                await using var source = File.OpenRead(localPath);
                await using var target = File.Create(destination);
                await source.CopyToAsync(target);

                return Path.Combine("Photo", "Chats", fileName).Replace('\\', '/');
            }
            catch
            {
                return null;
            }
        }

        private void UpdateHeaderSummaryForConversation(Conversation conversation)
        {
            _conversationTitle.Text = conversation.DisplayName;
            _conversationStatus.Text = BuildPresenceText(conversation.OtherIsOnline, conversation.OtherLastSeen);
        }

        private void UpdateHeaderSummary()
        {
            _headerTitle.Text = $"{_friendsList.Count} friends · {_incomingRequests.Count} pending requests · {_conversations.Count} chats";
            _headerSubtitle.Text = string.IsNullOrWhiteSpace(_searchBox.Text)
                ? "Use the search field to find people, then open a chat or send a request."
                : $"Showing search results for \"{_searchBox.Text.Trim()}\".";
        }

        private async Task OpenProfileAsync(int userId)
        {
            if (userId <= 0)
            {
                return;
            }

            await _openProfile(userId);
        }

        private Border MakeProfileAvatar(string? profileImage, string labelSource, double size, bool isOnline, int userId)
        {
            var avatar = CreateAvatarShell(profileImage, labelSource, size, isOnline);
            avatar.Cursor = new Cursor(StandardCursorType.Hand);
            avatar.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                await OpenProfileAsync(userId);
            };
            return avatar;
        }

        private TextBlock MakeProfileText(string text, int userId)
        {
            var block = new TextBlock
            {
                Text = text,
                FontSize = 13.5,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextPrimaryBrush,
                Cursor = new Cursor(StandardCursorType.Hand),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            block.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                await OpenProfileAsync(userId);
            };
            return block;
        }

        private static Border CreateAvatarShell(string? profileImage, string labelSource, double size, bool isOnline)
        {
            var avatar = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2),
                Background = ThemePalette.BgTertiaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            var container = new Grid();

            container.Children.Add(new TextBlock
            {
                Text = GetInitials(labelSource),
                FontSize = Math.Max(10, size * 0.28),
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextSecondaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            avatar.Child = container;

            _ = LoadAvatarAsync(avatar, profileImage);
            return avatar;
        }

        private static async Task LoadAvatarAsync(Border avatar, string? profileImage)
        {
            if (string.IsNullOrWhiteSpace(profileImage))
            {
                return;
            }

            var bitmap = await ImageLoader.LoadAsync(profileImage);
            if (bitmap == null)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                avatar.Child = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            });
        }

        private static string GetInitials(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "?";
            }

            var words = source
                .Split(new[] { ' ', '-', '_', '.', '@' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Take(2)
                .Select(w => char.ToUpperInvariant(w[0]))
                .ToArray();

            return words.Length == 0 ? "?" : new string(words);
        }

        private static string BuildPresenceText(bool isOnline, DateTime? lastSeen)
        {
            if (isOnline)
            {
                return "Online";
            }

            if (!lastSeen.HasValue)
            {
                return "Offline";
            }

            var diff = DateTime.UtcNow - lastSeen.Value.ToUniversalTime();
            if (diff.TotalMinutes < 1)
            {
                return "Last seen just now";
            }

            if (diff.TotalHours < 1)
            {
                return $"Last seen {Math.Max(1, (int)diff.TotalMinutes)} min ago";
            }

            if (diff.TotalDays < 1)
            {
                return $"Last seen {Math.Max(1, (int)diff.TotalHours)} h ago";
            }

            return $"Last seen {lastSeen.Value.ToLocalTime():dd MMM HH:mm}";
        }

        private static string FormatShortTime(DateTime value)
        {
            var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
            return local.ToString("HH:mm");
        }
    }
}
