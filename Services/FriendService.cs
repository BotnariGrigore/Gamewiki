using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class FriendService
    {
        private readonly FriendRepository _repo = new();
        private readonly UserRepository _users = new();
        private readonly NotificationService _notifications = new();

        public Task<IEnumerable<Friend>> GetFriendsAsync(int userId) => _repo.GetFriendsAsync(userId);
        public Task<IEnumerable<Friend>> GetIncomingRequestsAsync(int userId) => _repo.GetIncomingRequestsAsync(userId);
        public Task<IEnumerable<Friend>> GetOutgoingRequestsAsync(int userId) => _repo.GetOutgoingRequestsAsync(userId);
        public Task<IEnumerable<Friend>> SearchUsersAsync(int userId, string query, int limit = 20) => _repo.SearchUsersAsync(userId, query, limit);

        public async Task<bool> AreFriendsAsync(int userId, int otherUserId)
        {
            var rows = await _repo.GetRelationshipRowsAsync(userId, otherUserId);
            return rows.Any(row => row.IsAccepted);
        }

        public async Task<(bool success, string message)> SendRequestAsync(int userId, int friendId)
        {
            try
            {
                if (userId == friendId)
                {
                    return (false, "You cannot send a friend request to yourself.");
                }

                var target = await _users.GetByIdAsync(friendId);
                if (target == null)
                {
                    return (false, "The selected user does not exist.");
                }

                var rows = (await _repo.GetRelationshipRowsAsync(userId, friendId)).ToList();
                if (rows.Any(row => row.IsAccepted))
                {
                    return (false, "You are already friends.");
                }

                if (rows.Any(row => row.IsOutgoingRequest))
                {
                    return (false, "Friend request already sent.");
                }

                if (rows.Any(row => row.IsIncomingRequest))
                {
                    return (false, "This user already sent you a request. Accept it instead.");
                }

                if (rows.Any(row => row.IsBlocked))
                {
                    return (false, "This user is blocked.");
                }

                var id = await _repo.CreateRequestAsync(userId, friendId);
                if (id > 0)
                {
                    var sender = await _users.GetByIdAsync(userId);
                    if (sender != null)
                    {
                        await _notifications.NotifyFriendRequestAsync(sender, target);
                    }

                    return (true, "Friend request sent.");
                }

                return (false, "The friend request could not be created.");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("friend_errors.log", DateTime.UtcNow + " SEND REQUEST ERROR: " + ex + Environment.NewLine); } catch { }
                return (false, "An unexpected error occurred while sending friend request.");
            }
        }

        public async Task<(bool success, string message)> AcceptRequestAsync(int currentUserId, int friendshipId)
        {
            try
            {
                var rows = await _repo.GetIncomingRequestsAsync(currentUserId);
                if (!rows.Any(row => row.FriendshipId == friendshipId))
                {
                    return (false, "Friend request not found.");
                }

                var accepted = await _repo.AcceptRequestAsync(friendshipId, currentUserId);
                if (accepted)
                {
                    var request = rows.First(row => row.FriendshipId == friendshipId);
                    var acceptor = await _users.GetByIdAsync(currentUserId);
                    var requester = await _users.GetByIdAsync(request.OtherUserId);
                    if (acceptor != null && requester != null)
                    {
                        await _notifications.NotifyFriendAcceptedAsync(acceptor, requester);
                    }

                    return (true, "Friend request accepted.");
                }

                return (false, "The request could not be accepted.");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("friend_errors.log", DateTime.UtcNow + " ACCEPT REQUEST ERROR: " + ex + Environment.NewLine); } catch { }
                return (false, "An unexpected error occurred while accepting the request.");
            }
        }

        public async Task<(bool success, string message)> DeclineRequestAsync(int currentUserId, int friendshipId)
        {
            try
            {
                var rows = await _repo.GetIncomingRequestsAsync(currentUserId);
                if (!rows.Any(row => row.FriendshipId == friendshipId))
                {
                    return (false, "Friend request not found.");
                }

                var removed = await _repo.DeclineRequestAsync(friendshipId, currentUserId);
                return removed
                    ? (true, "Friend request declined.")
                    : (false, "The request could not be declined.");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("friend_errors.log", DateTime.UtcNow + " DECLINE REQUEST ERROR: " + ex + Environment.NewLine); } catch { }
                return (false, "An unexpected error occurred while declining the request.");
            }
        }

        public async Task<(bool success, string message)> RemoveFriendAsync(int userId, int otherUserId)
        {
            try
            {
                var removed = await _repo.DeleteRelationshipAsync(userId, otherUserId);
                return removed
                    ? (true, "Friend removed.")
                    : (false, "The relationship could not be removed.");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("friend_errors.log", DateTime.UtcNow + " REMOVE FRIEND ERROR: " + ex + Environment.NewLine); } catch { }
                return (false, "An unexpected error occurred while removing the friend.");
            }
        }
    }
}
