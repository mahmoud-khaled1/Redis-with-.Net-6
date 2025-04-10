using System.Text.Json;
using StackExchange.Redis;
using TestRedis;

namespace MyApp
{
    internal class Program
    {

        // Singleton ConnectionMultiplexer for Redis
        private static readonly Lazy<ConnectionMultiplexer> LazyConnection = new(() =>
        {
            return ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { { "redis-14087.c83.us-east-1-2.ec2.redns.redis-cloud.com", 14087 } },
                User = "default",
                Password = "272TPapqKTfSZAdGoZzUVqmarWeQm6JN"
            });
        });

        private static ConnectionMultiplexer Redis => LazyConnection.Value;
        private static IDatabase Db => Redis.GetDatabase();
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Connecting to Redis Cloud...");
                await Redis.GetDatabase().PingAsync(); // Test connection
                Console.WriteLine("Connected successfully!");

                // Run Redis operations
                // await DemoBasicOperations();
                // await DemoListOperations();
                // await DemoSetOperations();
                // await DemoHashOperations();
                // await DemoSortedSetOperations();
                Console.WriteLine("All operations completed successfully!");
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Redis connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Properly dispose of the connection (optional, as it's a singleton)
                // Redis.Close();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        // Demo basic key-value operations
        private static async Task DemoBasicOperations()
        {
            Console.WriteLine("\n--- Basic Operations ---");

            // Set a simple string value
            string key = "greeting";
            await Db.StringSetAsync(key, "Hello, Redis Cloud!");
            Console.WriteLine($"Set {key}: Hello, Redis Cloud!");

            // Get the value
            string value = await Db.StringGetAsync(key);
            Console.WriteLine($"Get {key}: {value}");

            // Set with expiration (10 seconds)
            await Db.StringSetAsync("tempKey", "Temporary Value", TimeSpan.FromSeconds(10));
            Console.WriteLine("Set tempKey with 10-second expiration");

            // Store and retrieve a JSON-serialized object
            var product = new Product { Id = 1, Name = "Laptop", Price = 999.99m };
            string productKey = "product:1";
            string json = JsonSerializer.Serialize(product);
            await Db.StringSetAsync(productKey, json);
            Console.WriteLine($"Set {productKey}: {json}");

            string retrievedJson = await Db.StringGetAsync(productKey);
            var retrievedProduct = JsonSerializer.Deserialize<Product>(retrievedJson);
            Console.WriteLine($"Get {productKey}: ID={retrievedProduct.Id}, Name={retrievedProduct.Name}, Price={retrievedProduct.Price}");

            // Delete a key
            await Db.KeyDeleteAsync(key);
            Console.WriteLine($"Deleted {key}");
        }
        // Demo list operations
        private static async Task DemoListOperations()
        {
            Console.WriteLine("\n--- List Operations ---");

            string listKey = "productList";
            await Db.KeyDeleteAsync(listKey); // Clear list for demo

            // Push items to a Redis List
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Laptop", Price = 999.99m },
                new Product { Id = 2, Name = "Phone", Price = 499.99m },
                new Product { Id = 3, Name = "Tablet", Price = 299.99m }
            };

            foreach (var product in products)
            {
                string json = JsonSerializer.Serialize(product);
                await Db.ListRightPushAsync(listKey, json);
                Console.WriteLine($"Pushed to {listKey}: {json}");
            }

            // Retrieve the list
            var storedItems = await Db.ListRangeAsync(listKey);
            Console.WriteLine($"Items in {listKey}:");
            foreach (var item in storedItems)
            {
                var product = JsonSerializer.Deserialize<Product>(item);
                Console.WriteLine($"ID: {product.Id}, Name: {product.Name}, Price: {product.Price}");
            }

            // Pop an item from the list (left side)
            string poppedJson = await Db.ListLeftPopAsync(listKey);
            var poppedProduct = JsonSerializer.Deserialize<Product>(poppedJson);
            Console.WriteLine($"Popped from {listKey}: ID={poppedProduct.Id}, Name={poppedProduct.Name}, Price={poppedProduct.Price}");
        }
        // Demo Redis Set operations
        private static async Task DemoSetOperations()
        {
            //In Redis, a Set is an unordered collection of unique strings.
            //It’s useful for scenarios where you need to store distinct items
            //and perform operations like adding, removing, checking membership,
            //or finding intersections/unions between sets.

            Console.WriteLine("\n--- Set Operations ---");

            // Define set keys
            string setKey1 = "users:team1";
            string setKey2 = "users:team2";

            // Clear sets for clean demo (optional)
            await Db.KeyDeleteAsync(new RedisKey[] { setKey1, setKey2 });

            // Add items to Set 1 (Team 1 members)
            string[] team1Members = { "Alice", "Bob", "Charlie" };
            foreach (var member in team1Members)
            {
                await Db.SetAddAsync(setKey1, member);
                Console.WriteLine($"Added '{member}' to {setKey1}");
            }

            // Add items to Set 2 (Team 2 members)
            string[] team2Members = { "Bob", "David", "Eve" };
            foreach (var member in team2Members)
            {
                await Db.SetAddAsync(setKey2, member);
                Console.WriteLine($"Added '{member}' to {setKey2}");
            }

            // Check membership
            string userToCheck = "Alice";
            bool isMember = await Db.SetContainsAsync(setKey1, userToCheck);
            Console.WriteLine($"'{userToCheck}' in {setKey1}? {isMember}");

            // Get all members of Set 1
            var team1MembersList = await Db.SetMembersAsync(setKey1);
            Console.WriteLine($"{setKey1} members:");
            foreach (var member in team1MembersList)
            {
                Console.WriteLine($"- {member}");
            }

            // Find intersection (common members between Team 1 and Team 2)
            var commonMembers = await Db.SetCombineAsync(SetOperation.Intersect, setKey1, setKey2);
            Console.WriteLine("Common members (intersection):");
            foreach (var member in commonMembers)
            {
                Console.WriteLine($"- {member}");
            }

            // Find union (all unique members across both teams)
            var allMembers = await Db.SetCombineAsync(SetOperation.Union, setKey1, setKey2);
            Console.WriteLine("All members (union):");
            foreach (var member in allMembers)
            {
                Console.WriteLine($"- {member}");
            }

            // Remove a member
            await Db.SetRemoveAsync(setKey1, "Charlie");
            Console.WriteLine($"Removed 'Charlie' from {setKey1}");

            // Get updated Set 1 members
            var updatedTeam1 = await Db.SetMembersAsync(setKey1);
            Console.WriteLine($"{setKey1} after removal:");
            foreach (var member in updatedTeam1)
            {
                Console.WriteLine($"- {member}");
            }
        }
        // Demo Redis Hash operations
        private static async Task DemoHashOperations()
        {
            //In Redis, a Hash is a data structure that maps fields (keys) to values
            //within a single Redis key. It’s similar to a dictionary or object
            //in programming languages, making it ideal for storing structured data
            //like records or entities with multiple attributes. Unlike Redis Sets
            //(unordered unique values) or Lists (ordered sequences),
            //Hashes allow you to store and manipulate key-value pairs under a single key efficiently.

            Console.WriteLine("\n--- Hash Operations ---");

            // Define a hash key for a user
            string hashKey = "user:1001";

            // Clear the hash for clean demo (optional)
            await Db.KeyDeleteAsync(hashKey);

            // Set individual fields in the hash
            await Db.HashSetAsync(hashKey, "name", "Alice Smith");
            await Db.HashSetAsync(hashKey, "email", "alice@example.com");
            await Db.HashSetAsync(hashKey, "age", "30");
            Console.WriteLine($"Set fields in {hashKey}: name=Alice Smith, email=alice@example.com, age=30");

            // Set multiple fields at once using HashEntry array
            var fields = new[]
            {
                new HashEntry("role", "Developer"),
                new HashEntry("lastLogin", DateTime.Now.ToString("yyyy-MM-dd"))
            };
            await Db.HashSetAsync(hashKey, fields);
            Console.WriteLine($"Set additional fields: role=Developer, lastLogin={DateTime.Now:yyyy-MM-dd}");

            // Get a single field
            string name = await Db.HashGetAsync(hashKey, "name");
            Console.WriteLine($"Get 'name' from {hashKey}: {name}");

            // Get all fields and values
            var allFields = await Db.HashGetAllAsync(hashKey);
            Console.WriteLine($"{hashKey} contents:");
            foreach (var field in allFields)
            {
                Console.WriteLine($"- {field.Name}: {field.Value}");
            }

            // Check if a field exists
            bool emailExists = await Db.HashExistsAsync(hashKey, "email");
            Console.WriteLine($"Does 'email' exist in {hashKey}? {emailExists}");

            // Update a field
            await Db.HashSetAsync(hashKey, "age", "31");
            Console.WriteLine($"Updated 'age' to 31");

            // Increment a numeric field
            long newAge = await Db.HashIncrementAsync(hashKey, "age", 1);
            Console.WriteLine($"Incremented 'age' by 1, new value: {newAge}");

            // Delete a field
            await Db.HashDeleteAsync(hashKey, "lastLogin");
            Console.WriteLine($"Deleted 'lastLogin' from {hashKey}");

            // Get updated hash contents
            var updatedFields = await Db.HashGetAllAsync(hashKey);
            Console.WriteLine($"{hashKey} after updates:");
            foreach (var field in updatedFields)
            {
                Console.WriteLine($"- {field.Name}: {field.Value}");
            }
        }
        // Demo Redis Sorted Set operations
        private static async Task DemoSortedSetOperations()
        {
            //In Redis, a Sorted Set (also called a ZSet) is a data structure that combines
            //the features of a Set (unique elements) with sorting capabilities.
            //Each element in a Sorted Set has an associated score, which determines
            //its order in the set. This makes Sorted Sets ideal for scenarios
            //where you need ranked or ordered data, such as leaderboards,
            //priority queues, or time-based rankings.

            Console.WriteLine("\n--- Sorted Set Operations ---");

            // Define a sorted set key for a leaderboard
            string sortedSetKey = "leaderboard";

            // Clear the sorted set for clean demo (optional)
            await Db.KeyDeleteAsync(sortedSetKey);

            // Add players with scores
            await Db.SortedSetAddAsync(sortedSetKey, "Alice", 1500);
            await Db.SortedSetAddAsync(sortedSetKey, "Bob", 2000);
            await Db.SortedSetAddAsync(sortedSetKey, "Charlie", 1800);
            Console.WriteLine("Added players: Alice (1500), Bob (2000), Charlie (1800)");

            // Add multiple entries at once
            var players = new[]
            {
                new SortedSetEntry("David", 1700),
                new SortedSetEntry("Eve", 1900)
            };
            await Db.SortedSetAddAsync(sortedSetKey, players);
            Console.WriteLine("Added players: David (1700), Eve (1900)");

            // Get all players in order (ascending by score)
            var allPlayersAsc = await Db.SortedSetRangeByRankWithScoresAsync(sortedSetKey, 0, -1, Order.Ascending);
            Console.WriteLine("Leaderboard (ascending):");
            foreach (var player in allPlayersAsc)
            {
                Console.WriteLine($"- {player.Element}: {player.Score}");
            }

            // Get top 3 players (descending by score)
            var topPlayers = await Db.SortedSetRangeByRankWithScoresAsync(sortedSetKey, 0, 2, Order.Descending);
            Console.WriteLine("Top 3 players (descending):");
            foreach (var player in topPlayers)
            {
                Console.WriteLine($"- {player.Element}: {player.Score}");
            }

            // Get rank of a player (0-based, ascending order)
            long? aliceRank = await Db.SortedSetRankAsync(sortedSetKey, "Alice", Order.Ascending);
            Console.WriteLine($"Alice's rank (ascending): {aliceRank}"); // 0-based index

            // Get score of a player
            double? bobScore = await Db.SortedSetScoreAsync(sortedSetKey, "Bob");
            Console.WriteLine($"Bob's score: {bobScore}");

            // Increment a player's score
            double newScore = await Db.SortedSetIncrementAsync(sortedSetKey, "Alice", 100);
            Console.WriteLine($"Incremented Alice's score by 100, new score: {newScore}");

            // Remove a player
            await Db.SortedSetRemoveAsync(sortedSetKey, "Charlie");
            Console.WriteLine("Removed Charlie from the leaderboard");

            // Get updated leaderboard (descending)
            var updatedPlayers = await Db.SortedSetRangeByRankWithScoresAsync(sortedSetKey, 0, -1, Order.Descending);
            Console.WriteLine("Updated leaderboard (descending):");
            foreach (var player in updatedPlayers)
            {
                Console.WriteLine($"- {player.Element}: {player.Score}");
            }
        }
    }
}