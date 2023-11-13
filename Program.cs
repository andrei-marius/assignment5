// Program.cs

Console.WriteLine("Welcome to the password-based authenticator");
Console.WriteLine("");

Authenticator auth = new Authenticator();

Console.WriteLine("Registering user admin");
auth.register("admin", "admindnc");


// the end-user interface
string? s = "x";

do {
  Console.Write("Please select character + enter\n"
          + "'r' (register)\n"
          + "'l' (login)\n"
          + "'sc' (safe composed query)\n"
          + "'c' (composed query)\n"
          + "'x' (exit)\n"
          + ">");
  s = Console.ReadLine();
  Console.WriteLine();
  switch (s) {
     case "r":
       register();
       break;
     case "l":
       login();
       break;
     case "x": 
       Console.WriteLine("exiting ..");
       break;
     case "sc":
       safeComposedQuery();
       break;
     case "c":
       composedQuery();
       break;
     default:
       Console.WriteLine("you typed " + "'" + s + "'" + " -- use a suggested value");
       break;
   } // end switch
} while (s != "x");

// register() and login()

void register() {
  Console.WriteLine("Registration .. ");
  string username = getUserInput("Please type username:");
  string password = getUserInput("Please type password:");
  bool registered = auth.register(username, password);
  if (registered) Console.WriteLine("Registration succeeded");
  else Console.WriteLine("Registration failed");
}

void login() {
  Console.WriteLine("Logging in .. ");
  string username = getUserInput("Please type username:");
  string password = getUserInput("Please type password:");
  bool loggedin = auth.login(username, password);
  if (loggedin) Console.WriteLine("Login succeeded");
  else Console.WriteLine("Login failed");
}

// helper functions for exit(), register() and login()

string getUserInput(string s) {
  Console.WriteLine(s);
  return Console.ReadLine() ?? readLineError();
}

string readLineError() {
  return "Error: no string read by Console.ReadLine()";
}

void safeComposedQuery()
{
    // defining the query
    Console.Write("Please type id of a course: ");
    string? user_defined = Console.ReadLine();
    string sql = $"select * from safe_course('{user_defined}')";

    // printing query string to console
    Console.Write("Query to be executed: " + sql + "\n");

    // executing query
    auth.query(sql);
}

void composedQuery()
{
    // defining the query
    string staticSQLbefore = "select * from course where course_id = '";
    Console.Write("Please type id of a course: ");
    string? user_defined = Console.ReadLine();
    string staticSQLafter = "' and dept_name != 'Biology'";
    string sql = staticSQLbefore + user_defined + staticSQLafter;

    // printing query string to console
    Console.Write("Query to be executed: " + sql + "\n");
    //Console.ForegroundColor = ConsoleColor.Red;
    //Console.Write(user_defined);
    //Console.ForegroundColor = ConsoleColor.White;
    //Console.WriteLine(staticSQLafter + "\n");

    // executing query
    auth.query(sql);
}





