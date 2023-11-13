// Authenticator.cs

using Npgsql; 

public class Authenticator {

  public Authenticator() {
    try {
      string s = "Host=localhost;Username=postgres;Password=postgres22;Database=university";
      con = new NpgsqlConnection(s);
      con.Open();
    } catch (Exception e) {
      Console.WriteLine("Exception caught in Authenticator.cs");
      Console.WriteLine("Could not connect to database");
      Console.WriteLine(e);
    }
    hashing = new Hashing();
  }
 
  protected NpgsqlConnection? con;
  public Hashing hashing; 

  public bool register(string username, string password) {
    // username can't be the empty string
    if (username.Length == 0) {
      Console.WriteLine("Username must contain at least one character");
      return false;
    }

    // check the password
    if (! passwordIsOK(password, username)) {
      Console.WriteLine("Password is too weak");
      return false;
    }
 
    // obtain hash + salt
    Tuple<string, string> hs = hashing.hash(password);
    string hashedpassword = hs.Item1; 
    string salt = hs.Item2;

    // add (username, salt, password) to table 'password'
    string sql = sqlInsertUserRecord(username, salt, hashedpassword);
    Console.WriteLine("SQL to be inserted: " + sql);
    NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
    cmd.CommandText = sql;
    try {
      cmd.ExecuteNonQuery(); 
    } catch (Exception e) {
      Console.WriteLine("Exception caught in Authenticator.cs");
      Console.WriteLine("Could not insert user record into table password");
      if (e.ToString().Contains("duplicate key")) {           // field username is primary key
        Console.WriteLine("Username has been taken already");
      }
      return false;
    }
      // Instead of ExecuteNonQuery I could use ExecuteReader(),
      // but ExecuteNonQuery is more simple since there is no result set
    return true;
  }

  public bool login(string username, string login_password) {
    string sql = sqlSelectUserRecord(username);
    NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
    NpgsqlDataReader rdr = cmd.ExecuteReader();
    bool userIsRegistered = rdr.HasRows;
    if (! userIsRegistered) {
      rdr.Close();
      return false;
    }
    rdr.Read(); // now access first row in result set
                // the row has the format ( salt, hashed_password )
    string salt = rdr.GetString(0);
    string hashed_registered_password = rdr.GetString(1); 
    rdr.Close();
    bool passwordIsVerified = hashing.verify(login_password, hashed_registered_password, salt);
    if (passwordIsVerified) return true;
    else {
        Console.WriteLine("Password supplied does not match stored password");
        return false;
      }
  } 

  // check the password
  // method passwordIsOK() is defined as virtual,
  // so that modifications may be implemented in a subclass
  // (but not required at all)

  public virtual bool passwordIsOK(string password, string username) {
    if (password.Length <= 8)
        {
            Console.WriteLine("Password must be longer than 8 characters");
            return false;
        }
            
    if (password.Contains(username))
        {
            Console.WriteLine("Password can not contain the username");
            return false;
        }

    return true;
  }

  // sqlSetUserRecord is used in register()

  virtual public string sqlInsertUserRecord(string username, string salt, string hashedpassword) {
    return "insert into password values ("
                     + "'" + username + "',"
                     + "'" + salt + "',"
                     + "'" + hashedpassword + "'"
                     + ")";
  }

  // sqlGetUserRecord is used in login()

  virtual public string sqlSelectUserRecord(string username) {
    return "select salt, hashed_password from password "
            + "where username = '" + username + "'";
  }

    public void query(string? sql)
    {
        try
        {
            NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
            if (cmd == null) Console.WriteLine("Error: database could not execute SQL query: " + sql);
            else
            {
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                int statements = countStatements(rdr);
                // the number of SQL statements in the query, usually one
                for (int s = 0; s < statements; s++)
                {
                    Table table = new Table(rdr);
                    table.print();
                    rdr.NextResult(); // advance to result of next SQL statement
                }
                rdr.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught by SQL-Injection-Frontend");
            string s = e.ToString().Substring(1, 23); // printing only first part of exc message
            Console.WriteLine(s + " ....");
            Console.WriteLine();
            Console.WriteLine(e.Message.ToString());
        }
    }

    private int countStatements(NpgsqlDataReader rdr)
    {
        // suppress the warning message saying NpgsqlDataReader.Statements is obsolete
#pragma warning disable 0618
        int s = rdr.Statements.Count;
#pragma warning restore 0618
        return s;
    }
} // end class PostgreSQ

/*****************************************************
* class Table                                        *
*                                                    * 
* An instance of class Table                         *
* is the result of a single sql query                *
*                                                    * 
* Normally it is a real table with headers and rows. *
*                                                    * 
* In some cases it has an empty lists of headers     *
* in which case it is interpreted and shown as:      *
* "(Query result is not a table)"                    *
*                                                    * 
******************************************************/

class Table
{
    public Table(NpgsqlDataReader rdr)
    {
        max = new int[rdr.FieldCount];
        headers = readHeaders(rdr, max);
        rowList = readRows(rdr, max);
    }

    private string[] headers;
    // column headers 
    private List<string[]> rowList;
    // finally, readiing the actual rows of the query's resultest
    private int[] max;
    // int[] max stores max #chars in a column, for formatting

    // methods for reading data from the NpgsqlDataReader

    private string[] readHeaders(NpgsqlDataReader rdr, int[] max)
    {
        int columns = max.Length;
        string[] ss = new string[columns];
        for (int c = 0; c < columns; c++)
        {
            ss[c] = rdr.GetName(c);  // rdr.GetName(c) returns the name of the c'th column
            int l = ss[c].Length;
            if (l > max[c]) max[c] = l;
        }
        return ss;
    }

    private List<string[]> readRows(NpgsqlDataReader rdr, int[] max)
    {
        List<string[]> rList = new List<string[]>();
        int columns = max.Length;
        while (rdr.Read())
        {
            string[] ss = new string[columns];
            for (int c = 0; c < columns; c++)
            {
                string typestring = rdr.GetFieldType(c).ToString();
                // before reading a field, the type of the field must be determined
                switch (typestring)
                {
                    case "System.String":
                        ss[c] = rdr.GetString(c);
                        break;
                    case "System.Decimal":
                        decimal d = rdr.GetDecimal(c);
                        ss[c] = d.ToString();
                        break;
                    default:
                        Console.WriteLine("Unknown field type: " + typestring
                                           + " .. field value is defaulted to the empty string");
                        ss[c] = "";
                        break;
                        //  if needed, add cases for System.Int16, System.Int32), System.Int64
                }
                int l = ss[c].Length;
                if (l > max[c]) max[c] = l;
            } // end for
            rList.Add(ss);
        }
        return rList;
    }

    // methods for printing

    public void print()
    {
        if (headers.Length == 0) Console.WriteLine("(Query result is not a table)");
        else
        {
            printHeaders();
            int rows = printRecords();
            if (rows == 1) Console.WriteLine("(1 row)");
            else Console.WriteLine("(" + rows + " rows)");
        }
        Console.WriteLine();
    }

    private void printHeaders()
    {
        int columns = max.Length;
        for (int c = 0; c < columns; c++)
        {
            Console.Write(" ");          // a border to the left
            Console.Write(headers[c]);   // the field value
                                         // and then spacing to the right:
            for (int s = 0; s < max[c] + 1 - headers[c].Length; s++) Console.Write(" ");
            if (c + 1 < columns)
            {         // separate from next column (if any)
                Console.Write("|");
            }
        }
        Console.WriteLine("");
        // printing a line of dashes to separate field names from records
        // -----------------------------------------------------
        for (int c = 0; c < columns; c++)
        {
            for (int d = 0; d < max[c] + 2; d++) Console.Write('-');
            if (c + 1 < columns) Console.Write("+");
        }
        Console.WriteLine("");
    } // end printHeaders

    private int printRecords()
    {
        int rows = rowList.Count;
        int columns = max.Length;
        for (int r = 0; r < rows; r++)
        {
            string[] row = rowList[r];
            for (int c = 0; c < columns; c++)
            {
                Console.Write(" ");
                Console.Write(row[c]);
                for (int s = 0; s < max[c] + 1 - row[c].Length; s++) Console.Write(" ");
                if (c + 1 < columns) Console.Write("|");
            }
            Console.WriteLine("");
        }
        return rows;
    } // end printRecords
}

