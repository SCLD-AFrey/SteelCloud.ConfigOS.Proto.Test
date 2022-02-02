using System;
using System.Collections.Generic;

namespace SteelCloud.ConfigOS.Proto.Test.Server
{
    public class UserMgmt
    {
        public List<User> UserRepo()
        {
            List<User> users = new List<User>();
            users.Add(new User() {Username = "arthur", Password = "password", Roles = new enmRole[] {enmRole.ADMIN, enmRole.POWER_USER, enmRole.READER}});
            users.Add(new User() {Username = "matt", Password = "password", Roles = new enmRole[] {enmRole.POWER_USER, enmRole.READER}});
            users.Add(new User() {Username = "jamie", Password = "password", Roles = new enmRole[] {enmRole.READER}});
            users.Add(new User() {Username = "public", Password = "password", Roles = new enmRole[] {enmRole.NONE}});
            return users;
        }
        public class User
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public enmRole[] Roles { get; set; } = new[] {enmRole.NONE};
        }
        [Flags]
        public enum enmRole
        {
            NONE = 0,
            ADMIN = 1,
            POWER_USER = 2,
            READER = 3
        }
    }
}