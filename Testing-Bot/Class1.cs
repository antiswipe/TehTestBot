using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using System.Configuration;
using Discord.Audio;
using Sodium;
using Microsoft.Extensions.DependencyInjection;


namespace Testing_Bot
{
    public class Program
    {
        private readonly IServiceCollection _map = new ServiceCollection();
        private CommandService _commands;
        private IServiceProvider _services;
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            //client.Log
            _client.MessageReceived += MessageReceived;

            _client.Log += Log;

            string token = "Mzg0ODkyMzE5NTY1ODczMTUz.DP5kEg.9hfrboDviwY8c7as00evoKqiFb4";
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            await InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessagedReceived Event into our Command Handler
            _client.MessageReceived += HandleCommandAsync;
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        // Create a module with no prefix
        public class InfoModule : ModuleBase<SocketCommandContext>
        {
            [Command("say")]
            [Summary("Echos a message.")]
            public async Task SayAsync([Remainder] [Summary("This is test text")] string echo)
            {
                // ReplyAsync is a method on ModuleBase
                await ReplyAsync(echo);
            }

            [Command("join")]
            public async Task JoinChannel(IVoiceChannel channel = null)
            {
                // Get the audio channel
                var msg = 
                channel = channel ?? (msg.Author as IGuildUser)?.VoiceChannel;
                if (channel == null) { await msg.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

                // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
                var audioClient = await channel.ConnectAsync();
            }
        }

        [Group("Testing")]
        public class Testing : ModuleBase<SocketCommandContext>
        {
            // ~testing square 20 -> 400
            [Command("square")]
            [Summary("Squares a number.")]
            public async Task SquareAsync([Summary("The number to square.")] int num)
            {
                // We can also access the channel from the Command Context.
                await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
            }

            // ~testing userinfo --> Testing-Bot#7695
            // ~testing userinfo @Antiswipe --> Antiswipe#3594
            // ~testing userinfo Antiswipe#3594 --> Antiswipe#3594
            // ~testing userinfo Antiswipe --> Antiswipe#3594
            // ~testing userinfo 384892319565873153 --> Testing-Bot#7695
            // ~testing whois 384892319565873153 --> Testing-Bot#7695
            [Command("userinfo")]
            [Summary("Returns info about the current user, or the user parameter, if one passed.")]
            [Alias("user", "whois")]
            public async Task UserInfoAsync([Summary("The (optional) user to get info for")] SocketUser user = null)
            {
                var userInfo = user ?? Context.Client.CurrentUser;
                await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
            }
        }
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new SocketCommandContext(_client, message);
            // Execute the command. (result does not indicate a return value, rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            if (message.Content == "chrisIsAwesome")
            {
                await message.Channel.SendMessageAsync("Amen!");
            }
        }

        private Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            return Task.CompletedTask;
        }
    }
}
