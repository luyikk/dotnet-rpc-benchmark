// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace GreeterServer
{
    class GreeterImpl : Greeter.GreeterBase
    {

        public async override Task<World> Get(NullRequest request, ServerCallContext context)
        {
            using (var db = Npgsql.NpgsqlFactory.Instance.CreateConnection())
            {
                db.ConnectionString = Program.ConnectionString;
                var cmd = db.CreateCommand();
                cmd.CommandText = "SELECT id, randomnumber FROM world WHERE id = @Id";
                var id = cmd.CreateParameter();
                id.ParameterName = "@Id";
                id.DbType = DbType.Int32;
                id.Value = new Random().Next(1, 10001);
                cmd.Parameters.Add(id);
                await db.OpenAsync();
                using (var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    await rdr.ReadAsync();
                    return new World
                    {
                        Id = rdr.GetInt32(0),
                        RandomNumber = rdr.GetInt32(1)
                    };
                }
            }
        }



        public async override Task<ListReply> ListDB(NullRequest request, ServerCallContext context)
        {
            ListReply reply = new ListReply();
            using (var db = Npgsql.NpgsqlFactory.Instance.CreateConnection())
            {
                db.ConnectionString = Program.ConnectionString;
                var cmd = db.CreateCommand();
                cmd.CommandText = "SELECT id, message FROM fortune";
                var id = cmd.CreateParameter();
                id.ParameterName = "@Id";
                id.DbType = DbType.Int32;
                id.Value = new Random().Next(1, 10001);
                cmd.Parameters.Add(id);
                await db.OpenAsync();
                using (var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                {
                    while (await rdr.ReadAsync())
                    {
                        reply.Items.Add(new Fortune
                        {
                            Id = rdr.GetInt32(0),
                            Message = rdr.GetString(1)
                        });
                    }
                }
            }
            return reply;
        }


        public  override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }

        public override Task<User> Register(RegisterRequest request, ServerCallContext context)
        {
            return Task.FromResult(new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = request.Password,
                Title = request.Title,
                City = request.City,
                CreateTime = DateTime.Now.Ticks,
                ID = Guid.NewGuid().ToString("N")

            });
        }

        public override Task<SearchReply> List(SearchRequest request, ServerCallContext context)
        {
            SearchReply result = new SearchReply();
            for (int i = 0; i < request.Count; i++)
            {
                var item = new User
                {
                    Name = "henryfan",
                    City = "guangzhou",
                    Email = "henryfan@msn.com",
                    Title = "cxo",
                    Password = "12345678",
                    ID = Guid.NewGuid().ToString("N"),
                    CreateTime = DateTime.Now.Ticks
                };
                result.Items.Add(item);
            }
            return Task.FromResult(result);
        }
    }

    class Program
    {
        const int Port = 50051;

        public static string ConnectionString = "";

        public static void Main(string[] args)
        {
            string host = "127.0.0.1";
            if (args != null && args.Length > 0)
                host = args[0];
            string dbHost = "127.0.0.1";
            if (args != null && args.Length > 1)
                dbHost = args[1];
            ConnectionString = $"Server={dbHost};Database=hello_world;User Id=benchmarkdbuser;Password=benchmarkdbpass;Maximum Pool Size=256;NoResetOnClose=true;Enlist=false;Max Auto Prepare=3";
            Console.WriteLine($"Server Host:{host}| DB Host:{dbHost}");
            Server server = new Server
            {
                Services = { Greeter.BindService(new GreeterImpl()) },
                Ports = { new ServerPort(host, Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Greeter server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
