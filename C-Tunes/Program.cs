using DSharpPlus;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using C_Tunes;

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
        LavaLinkUtils lavaUtil = new LavaLinkUtils();
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
                
                await lavaUtil.addSong(ctx, String.Join(" ", song));
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

        [Command("q")]
        public async Task Queue(CommandContext ctx)
        {
            Console.WriteLine("Queue");
            ctx.RespondAsync(lavaUtil.GetMusicQueue());
        }
    }

    public class LavaLinkUtils
    {
        private TrackQueue myQueue = new TrackQueue(true);
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

            if(myQueue.IsEmpty())
            {
                Console.WriteLine("First song");
                myQueue.AddTrack(track);
                playSong(conn, ctx);
            } else
            {
                Console.WriteLine("Next song");
                myQueue.AddTrack(track);
            }
        }

        private async void playSong(LavalinkGuildConnection conn, CommandContext ctx)
        {
            LavalinkTrack track = myQueue.GetNext();
            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing {track.Title}!");
            

            conn.PlaybackFinished += async (sender, e) =>
            {
                if(myQueue.QueueExists())
                {
                    LavalinkTrack track = myQueue.GetNext();
                    await conn.PlayAsync(track);
                    await ctx.RespondAsync($"Now playing {track.Title}!");
                }
                
            };
        }

        public string GetMusicQueue()
        {
            return myQueue.GetQueue();
        }
    }

    public class Config
    {
        public string Token { get; set; }
    }
}