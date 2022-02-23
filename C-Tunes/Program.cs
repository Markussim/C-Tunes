using DSharpPlus;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;


namespace MyFirstBot
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Directory.GetCurrentDirectory();
            Console.WriteLine(path);
            DotNetEnv.Env.Load("../../../.env");
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {

            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            var discord = new DiscordClient(new DiscordConfiguration()
            {

                Token = Environment.GetEnvironmentVariable("TOKEN"),
                TokenType = TokenType.Bot
            });

            var lavalink = discord.UseLavalink();

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
{
                StringPrefixes = new[] { "ö" }
            });

            commands.RegisterCommands<Commands>();

            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig); // Make sure this is after Discord.ConnectAsync(). 
            await Task.Delay(-1);
        }
    }

    public class Commands : BaseCommandModule
    {
        [Command]
        public async Task play(CommandContext ctx, params string[] song)
        {
            if(ctx.Member.VoiceState.Channel != null)
            {
                DiscordChannel channel = ctx.Member.VoiceState.Channel;
                Console.WriteLine("Pucko join");

                var lava = ctx.Client.GetLavalink();
                if (!lava.ConnectedNodes.Any())
                {
                    await ctx.RespondAsync("The Lavalink connection is not established, pucko");
                    return;
                }

                var node = lava.ConnectedNodes.Values.First();

                if (channel.Type != ChannelType.Voice)
                {
                    await ctx.RespondAsync("Not a valid voice channel, pucko.");
                    return;
                }

                await node.ConnectAsync(channel);
                LavaLinkUtils lavaa = new LavaLinkUtils();
                await lavaa.addSong(ctx, String.Join(" ", song));
            } else
            {
                await ctx.RespondAsync("You're not in a channel, pucko");
            }
            
        }

        [Command]
        public async Task Leave(CommandContext ctx)
        {
            if (ctx.Member.VoiceState.Channel != null)
            {
                DiscordChannel channel = ctx.Member.VoiceState.Channel;
                var lava = ctx.Client.GetLavalink();
                if (!lava.ConnectedNodes.Any())
                {
                    await ctx.RespondAsync("The Lavalink connection is not established, pucko");
                    return;
                }

                var node = lava.ConnectedNodes.Values.First();

                if (channel.Type != ChannelType.Voice)
                {
                    await ctx.RespondAsync("Not a valid voice channel, pucko.");
                    return;
                }

                var conn = node.GetGuildConnection(channel.Guild);

                if (conn == null)
                {
                    await ctx.RespondAsync("Lavalink is not connected.");
                    return;
                }

                await conn.DisconnectAsync();
                await ctx.RespondAsync($"Left {channel.Name}!");
            }
                
        }

        [Command("pucko")]
        public async Task pucko(CommandContext ctx)
        {
            Console.WriteLine("Pucko pucko!");
            await ctx.RespondAsync("Pucko to you too!");
        }
    }

    public class LavaLinkUtils
    {
        private Queue queue = new Queue();
        public async Task addSong(CommandContext ctx, string song)
        {
            //Important to check the voice state itself first, 
            //as it may throw a NullReferenceException if they don't have a voice state.
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(song);

            //If something went wrong on Lavalink's end                          
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed

                //or it just couldn't find anything.
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {song}.");
                return;
            }

            var track = loadResult.Tracks.First();

            if(queue.IsEmpty())
            {
                Console.WriteLine("First song");
                queue.AddTrack(track);
                playSong(conn, ctx);
            } else
            {
                Console.WriteLine("Next song");
                queue.AddTrack(track);
            }
        }

        private async void playSong(LavalinkGuildConnection conn, CommandContext ctx)
        {
            LavalinkTrack track = queue.GetNext();
            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing {track.Title}!");
            

            conn.PlaybackFinished += async (sender, e) =>
            {
                if(queue.QueueExists())
                {
                    LavalinkTrack track = queue.GetNext();
                    await conn.PlayAsync(track);
                    await ctx.RespondAsync($"Now playing {track.Title}!");
                }
                
            };
        }

        public class Config
        {
            public string Token { get; set; }
        }
    }

    public class Queue
    {
        List<LavalinkTrack> tracks = new List<LavalinkTrack>();
        LavalinkTrack? currentTrack = null;
        public Queue()
        {

        }

        public bool IsEmpty()
        {
            Console.WriteLine(tracks);
            Console.WriteLine(currentTrack);
            return tracks.Count == 0 && currentTrack == null;
        }

        public bool QueueExists()
        {
            return tracks.Count > 0;
        }

        public LavalinkTrack GetNext()
        {
            currentTrack = tracks[0];
            tracks.RemoveAt(0);
            return currentTrack;
        }

        public LavalinkTrack GetSafeNext()
        {
            return currentTrack;
        }

        public void AddTrack(LavalinkTrack track)
        {
            tracks.Add(track);
            Console.WriteLine("Added song, queue length is now " + tracks.Count);
        }
    }
}