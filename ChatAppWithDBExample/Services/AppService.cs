using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ChatAppWithDBExample.Hubs;
using ChatAppWithDBExample.Models;

namespace ChatAppWithDBExample.Services
{
    public class AppService
    {
        ChatAppDBEntities _Context;

        public AppService()
        {
            _Context = new ChatAppDBEntities();
        }

        public bool Login(LoginData loginData, out int userId)
        {
            userId = 0;
            var currentUser = _Context.Users.FirstOrDefault(x => x.Username == loginData.Username && x.Password == loginData.Password);
            if (currentUser != null)
            {
                userId = currentUser.UserId;
                return true;
            }
            return false;
        }

        public List<UserDTO> GetUsersToChat()
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            return _Context.Users
                .Include("UserConnections")
                .Where(x => x.UserId != userId)
                .Select(x => new UserDTO
                {
                    UserId = x.UserId,
                    UserName = x.Username,
                    FullName = x.FullName,
                    IsOnline = x.UserConnections.Count > 0,
                }).ToList();
        }

        internal int AddUserConnection(Guid ConnectionId)
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            _Context.UserConnections.Add(new UserConnection
            {
                ConnectionId = ConnectionId,
                UserId = userId,
            });
            _Context.SaveChanges();
            return userId;
        }

        internal int RemoveUserConnection(Guid ConnectionId)
        {
            int userId = 0;
            var current = _Context.UserConnections.FirstOrDefault(x => x.ConnectionId == ConnectionId);
            if (current != null)
            {
                userId = current.UserId ?? 0;
                _Context.UserConnections.Remove(current);
                _Context.SaveChanges();
            }
            return userId;
        }

        internal IList<string> GetUSerConnections(int uSerId)
        {
            return _Context.UserConnections.Where(x => x.UserId == uSerId).Select(x => x.ConnectionId.ToString()).ToList();
        }

        internal void RemoveAllUserConnections(int userId)
        {
            var current = _Context.UserConnections.Where(x => x.UserId == userId);
            _Context.UserConnections.RemoveRange(current);
            _Context.SaveChanges();
        }

        internal ChatBoxModel GetChatbox(int toUserId)
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            var toUser = _Context.Users.FirstOrDefault(x => x.UserId == toUserId);
            var messages = _Context.Messages.Where(x => (x.FromUser == userId && x.ToUser == toUserId) || (x.FromUser == toUserId && x.ToUser == userId))
                .OrderByDescending(x => x.Date)
                .Skip(0)
                .Take(50)
                .Select(x => new MessageDTO
                {
                    ID = x.Id,
                    Message = x.Message1,
                    Class = x.FromUser == userId ? "from" : "to",
                })
                .OrderBy(x => x.ID)
                .ToList();

            return new ChatBoxModel
            {
                ToUser = ToUserDTO(toUser),
                Messages = messages,
            };
        }

        internal bool SendMessage(int toUserId, string message)
        {
            try
            {
                int USER_ID = int.Parse(HttpContext.Current.User.Identity.Name);
                _Context.Messages.Add(new Message
                {
                    FromUser = USER_ID,
                    ToUser = toUserId,
                    Message1 = message,
                    Date = DateTime.Now
                });
                _Context.SaveChanges();
                ChatHub.RecieveMessage(USER_ID, toUserId, message);
                return true;
            }
            catch { return false; }
        }

        public UserDTO ToUserDTO(User user)
        {
            return new UserDTO
            {
                FullName = user.FullName,
                UserId = user.UserId,
                UserName = user.Username,
            };
        }


        internal List<MessageDTO> LazyLoadMssages(int toUserId, int skip)
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            var messages = _Context.Messages.Where(x => (x.FromUser == userId && x.ToUser == toUserId) || (x.FromUser == toUserId && x.ToUser == userId))
                .OrderByDescending(x => x.Date)
                .Skip(skip)
                .Take(50)
                .Select(x => new MessageDTO
                {
                    ID = x.Id,
                    Message = x.Message1,
                    Class = x.FromUser == userId ? "from" : "to",
                })
                .OrderByDescending(x => x.ID)
                .ToList();
            return messages;
        }
    }
}
