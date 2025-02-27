using System.Xml.Serialization;

namespace SharpFtpServer
{
    public static class UserStore
    {
        private static List<User> _users;

        static UserStore()
        {
            _users = new List<User>();

            XmlSerializer serializer = new XmlSerializer(_users.GetType(), new XmlRootAttribute("Users"));

            if (File.Exists("users.xml"))
            {
                _users = serializer.Deserialize(new StreamReader("users.xml")) as List<User>;
            }
            else
            {
                _users.Add(new User {
                    Username = "admin",
                    Password = "admin",
                    HomeDir = Path.GetFullPath("Log")
                });

                using (StreamWriter w = new StreamWriter("users.xml"))
                {
                    serializer.Serialize(w, _users);
                }
            }
        }

        public static User Validate(string username, string password)
        {
            User user = (from u in _users where u.Username == username && u.Password == password select u).SingleOrDefault();

            return user;
        }
    }
}
