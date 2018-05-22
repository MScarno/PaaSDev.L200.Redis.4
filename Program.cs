using System;
using System.Configuration;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PaaSDevL200Redis4
{
    class Program
    {
        static void Main(string[] args)
        {
            int workerThreads, IOThreads = 0;

            //Connect to redis
            string connectionString = ConfigurationManager.ConnectionStrings["RedisConnectionString"].ConnectionString;
            ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(connectionString);
            IDatabase db = connection.GetDatabase();
            var anyServer = connection.GetServer(connection.GetEndPoints()[0]);
            anyServer.Ping();
            var config = anyServer.ClusterConfiguration;

            // Threadpool part
            System.Threading.ThreadPool.GetMinThreads(out workerThreads, out IOThreads);
            Console.WriteLine("MinThreadPool values used : {0} Worker and {1} IO", workerThreads, IOThreads);

            // invent 2 keys - This part of the code should not be edited
            string x = Guid.NewGuid().ToString(), y;
            var xNode = config.GetBySlot(x);
            int abort = 1000;
            do
            {
                y = Guid.NewGuid().ToString();
            } while (--abort > 0 && config.GetBySlot(y) != xNode);
            if (abort == 0)
            {
                Console.WriteLine("Failed to find a different node to use, start the program again");
                Console.ReadLine();
                return;
            }
            var yNode = config.GetBySlot(y);
            Console.WriteLine("- Keys x and y got generated succesfully :\nx={0}\ny={1}", x, y);
            // Keys got generated successfully - Code that is below can be edited
            try
            { 
                // wipe those keys in case they got added before
                db.KeyDelete(x, CommandFlags.FireAndForget);
                db.KeyDelete(y, CommandFlags.FireAndForget);
                //Setting the keys
                var tran = db.CreateTransaction();
                tran.AddCondition(Condition.KeyNotExists(x));
                tran.AddCondition(Condition.KeyNotExists(y));
                var setX = tran.StringSetAsync(x, "Good");
                var setY = tran.StringSetAsync(y, "Job");
                //Trying to GET those keys
                RedisKey[] keysToGet = new RedisKey[] { x, y };
                Task<RedisValue[]> res = db.StringGetAsync(keysToGet);
                //Printing the result
                Console.WriteLine("Key x : {0}\nKey y : {1}", res.Result[0].ToString(), res.Result[1].ToString());
                Console.WriteLine("\n- Keys got retrieved without issues. Press a key to continue -");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("- Exceptions occurred -\n");
                // Handle the custom exception.
                Console.WriteLine(e.ToString() + "\n");
                Console.WriteLine("\n- You faced an exception -\n Press a button to exit -");
                Console.ReadLine();
            }

        }
    }
}
