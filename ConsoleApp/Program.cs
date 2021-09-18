using System;
using System.Data.SqlClient;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var constring = "Data source=localhost; Initial catalog=HomeWork16_16.09.2021; Integrated security=true";
            SqlConnection connection = new SqlConnection(constring);
            connection.Open();
            if (!(connection.State == System.Data.ConnectionState.Open))
            {
                return;

            }
            var loop = true;
            while (loop)
            {
                Console.Write("Select your option:\n\n1. Create account;\n2. Show accounts;\n3. Transfer from one account to another;\n4. Exit.\n\nYour choice: ");
                var choice = int.Parse(Console.ReadLine());

                switch (choice)
                {
                    case 1:
                        CreateAccount(connection);
                        break;

                    case 2:
                        ShowAccounts(connection);
                        break;

                    case 3:
                        Transfer(connection);
                        break;

                    case 4:
                        loop = false;
                        break;


                }
                Console.WriteLine("Press any key...");
                Console.ReadLine();
                Console.Clear();
            }

            connection.Close();
            
        }

        static void CreateAccount(SqlConnection connection)
        {
            Console.WriteLine("Desired 5-digit account number:");
            string account = Console.ReadLine();
            Console.WriteLine("How much money would you like to put on your account?");
            int.TryParse(Console.ReadLine(), out var balance);
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Account (Account_Number, Created_At, Balance) VALUES (@account, @created_at, @balance)";
            command.Parameters.AddWithValue("@account", account);
            command.Parameters.AddWithValue("@created_at", DateTime.Now);
            command.Parameters.AddWithValue("@balance", balance);
            var reader = command.ExecuteNonQuery();
            command.Parameters.Clear();
            if (reader == 0)
            {
                Console.WriteLine("Operation was not succeeded");
            }

            else
            {
                Console.WriteLine("Account successfully created");
            }



        }
        static void ShowAccounts(SqlConnection connection)
        {

            Account[] accounts = new Account[0];
            var command = connection.CreateCommand();
            command.CommandText = "SELECT [Id], [Account_Number], [Is_Active], [Created_At], [Updated_At], [Balance] FROM [dbo].[Account];";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var account = new Account { };
                account.Id = int.Parse(reader["Id"].ToString());
                account.Account_number = reader["Account_Number"].ToString();
                account.Is_Active = int.Parse(reader["Is_Active"].ToString());
                account.Created_At = !string.IsNullOrEmpty(reader["Created_At"]?.ToString()) ? DateTime.Parse(reader["Created_At"].ToString()) : null;
                account.Updated_At = !string.IsNullOrEmpty(reader["Updated_At"]?.ToString()) ? DateTime.Parse(reader["Updated_At"].ToString()) : null;
                account.Balance = decimal.Parse(reader["Balance"].ToString());
                AddAccount(ref accounts, account);
            }
            
            foreach (var account in accounts)
            {
                Console.WriteLine($"ID: {account.Id}, Account number: {account.Account_number}, Is Active: {account.Is_Active}, Created at: {account.Created_At}, Updated at: {account.Updated_At}, Balance: {Math.Round(account.Balance, 0)} $");
            }
            reader.Close();
        }

        static void AddAccount(ref Account[] accounts, Account account)
        {
            if (accounts == null)
            {

                return;
            }
            Array.Resize(ref accounts, accounts.Length + 1);
            accounts[accounts.Length - 1] = account;
        }

        static void Transfer(SqlConnection connection)
        {
            SqlTransaction transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            try
            {
                Console.WriteLine("From account: ");
                string fromacc = Console.ReadLine();
                Console.WriteLine("To account: ");
                string toacc = Console.ReadLine();
                Console.WriteLine("Amount: ");
                var amount = decimal.Parse(Console.ReadLine());
                if (string.IsNullOrEmpty(fromacc) || string.IsNullOrEmpty(toacc) || amount == 0)
                {
                    Console.WriteLine("Incorrect input");
                    return;
                }
                var fromAccBalance = GetAccountBalance(connection, fromacc);
                if (fromAccBalance < 0 || (fromAccBalance - amount) < 0)
                {
                    Console.WriteLine("Insufficient funds on balance");
                    return;
                }

                
                command.CommandText = "INSERT INTO Account (Balance) VALUES (Balance - @balance) WHERE Account_Number = @fromaccount";
                command.Parameters.AddWithValue("@balance", amount);
                command.Parameters.AddWithValue("@fromaccount", fromacc);
                var credit = command.ExecuteNonQuery();
                command.Parameters.Clear();
                if (credit == 0)
                {
                    Console.WriteLine("Withdrawal was not succeeded");
                    return;
                }

                
                command.CommandText = "INSERT INTO Account (Balance) VALUES (Balance + @balance) WHERE Account_Number = @toaccount";
                command.Parameters.AddWithValue("@balance", amount);
                command.Parameters.AddWithValue("@toaccount", toacc);
                var debit = command.ExecuteNonQuery();
                command.Parameters.Clear();

                if (debit == 0)
                {
                    Console.WriteLine("Replenishment was not succeeded");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
            }
        }

        static decimal GetAccountBalance (SqlConnection connection, string account)
        {
            decimal balance = 0m;
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Balance FROM Account WHERE Account_Number = @account";
            command.Parameters.AddWithValue("@account", account);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                balance = !string.IsNullOrEmpty(reader.GetValue(0)?.ToString()) ? reader.GetDecimal(0) : 0;
            }
            command.Parameters.Clear();
            reader.Close();
            return balance;
        }
 
    }

    public class Account
    {
        public int Id { get; set; }
        public string Account_number { get; set; }
        public int Is_Active { get; set; }
        public DateTime? Created_At { get; set; }
        public DateTime? Updated_At { get; set; }
        public decimal Balance { get; set; }
    }

    class Transaction
    {
        public int Id { get; set; }
        public int Account_Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Created_At { get; set; }

    }
}
