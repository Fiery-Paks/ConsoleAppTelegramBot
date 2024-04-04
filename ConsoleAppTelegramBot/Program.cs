using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ConsoleAppTelegramBot.Models;

internal class Program
{
    private static void Main(string[] args)
    {
        Start();
        Console.ReadLine();
    }
    private static async void Start()
    {
        var botClient = new TelegramBotClient(ConsoleAppTelegramBot.Properties.Resources.Token);

        using CancellationTokenSource cts = new();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await HandleCallBackAsync(botClient, update, cancellationToken);
        await HandlePhotoAsync(botClient, update, cancellationToken);
        await HandleMessageAsync(botClient, update, cancellationToken);
    }
    private static async Task HandleCallBackAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update == null || update.CallbackQuery == null)
        {
            return;
        }

        TelegramBotTestContext context = new TelegramBotTestContext();
        ConsoleAppTelegramBot.Models.User finduser = context.Users.FirstOrDefault(x => x.Idtelegram == update.CallbackQuery.Message!.Chat.Id)!;
        if (finduser == null)
        {
            ConsoleAppTelegramBot.Models.User user = new ConsoleAppTelegramBot.Models.User();
            user.FullName = update.CallbackQuery.Message!.Chat.Username!;
            user.Idtelegram = update.CallbackQuery.Message!.Chat.Id;
            user.NubmerPc = int.Parse(update.CallbackQuery.Data!);
            user.Wave = context.Waves.OrderByDescending(x => x.Id).First().Id;
            context.Users.Add(user);
            await context.SaveChangesAsync();


            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[] { new KeyboardButton[] { "Дай мне фото" }, }) { ResizeKeyboard = true };

            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery.Message!.Chat.Id,
                text: "Данные сохранены!",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);

            await AdminSending(botClient, update, cancellationToken, user);
        }
    }
    private static async Task AdminSending(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, ConsoleAppTelegramBot.Models.User user)
    {
        TelegramBotTestContext testContext = new TelegramBotTestContext();
        var listadmin = testContext.Admins.ToList();
        foreach (var item in listadmin)
        {
            await botClient.SendTextMessageAsync(
                chatId: item.Idtelegram,
                text: $@"Пользователь {user.Idtelegram} {user.FullName} на PC{user.NubmerPc} добавил данные!",
                cancellationToken: cancellationToken);
        }
    }
    private static async Task AdminSendingWave(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, int wave)
    {
        TelegramBotTestContext testContext = new TelegramBotTestContext();
        var listadmin = testContext.Admins.ToList();
        foreach (var item in listadmin)
        {

            await botClient.SendTextMessageAsync(
               chatId: item.Idtelegram,
               text: $"Перешли на волну {wave}",
               cancellationToken: cancellationToken);
        }
    }
    private static async Task AdminSendingPhotoAdd(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, ConsoleAppTelegramBot.Models.User user)
    {
        TelegramBotTestContext testContext = new TelegramBotTestContext();
        var listadmin = testContext.Admins.ToList();
        foreach (var item in listadmin)
        {
            await botClient.SendTextMessageAsync(
               chatId: item.Idtelegram,
               text: $"Фото для ползователя {user.FullName} {user.Idtelegram} волна {user.Wave} загружено!",
               cancellationToken: cancellationToken);
        }
    }

    private static async Task HandlePhotoAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update == null || update.Message == null || update.Message.Photo == null || update.Message!.Caption == null)
        {
            return;
        }
        await DowloadPhoto(botClient, update, cancellationToken);
    }
    private static async Task DowloadPhoto(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        TelegramBotTestContext testContext = new TelegramBotTestContext();
        var listadmin = testContext.Admins.ToList();
        if (listadmin.Any(x => x.Idtelegram == update.Message!.Chat.Id))
        {
            if (update.Message!.Caption!.ToLower().Contains("pc"))
            {
                var numberpc = int.Parse(update.Message!.Caption.ToLower().Replace("pc", ""));
                var fileId = update.Message!.Photo!.Last().FileId;
                var fileInfo = await botClient.GetFileAsync(fileId);
                var filePath = fileInfo.FilePath;

                string url = @$"https://api.telegram.org/file/bot{ConsoleAppTelegramBot.Properties.Resources.Token}/{filePath}";
                string newnamefile = $@"{Thread.CurrentThread.ManagedThreadId}{Path.GetFileName(filePath!)}";
                string localpath = @$"Images\{newnamefile}";

                using (var client = new HttpClient())
                {
                    using (var s = client.GetStreamAsync(url))
                    {
                        using (var fs = new FileStream(localpath, FileMode.OpenOrCreate))
                        {
                            s.Result.CopyTo(fs);
                        }
                    }
                }
                int wavelast = testContext.Waves.OrderByDescending(x => x.Id).First().Id;
                ConsoleAppTelegramBot.Models.User user = testContext.Users.FirstOrDefault(x => x.NubmerPc == numberpc && x.Wave == wavelast)!;
                if (user == null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: $"Данного пользователя не найденно",
                        cancellationToken: cancellationToken);
                    return;
                }
                user.Image = System.IO.File.ReadAllBytes(localpath);
                await testContext.SaveChangesAsync();

                System.IO.File.Delete(localpath);
                await AdminSendingPhotoAdd(botClient, update, cancellationToken, user);
            }
            return;
        }



    }
    private static async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        TelegramBotTestContext testContext = new TelegramBotTestContext();
        var listadmin = testContext.Admins.ToList();
        if (listadmin.Any(x => x.Idtelegram == update.Message!.Chat.Id))
        {
            await AdminMessageHeandler(botClient, update, cancellationToken);
            return;
        }

        if (message.Text == "/start")
        {
            InlineKeyboardButton[][] inlines = new InlineKeyboardButton[6][];
            for (int i = 1; i < 13; i += 2)
            {
                var buttons = new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData(text: $"PC{i}", callbackData: $"{i}"),
                    InlineKeyboardButton.WithCallbackData(text: $"PC{i+1}", callbackData: $"{i+1}"),
                };
                inlines[(i - 1) / 2] = buttons;
            }
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(inlines);
            await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Привет выбери номер своего ПК",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
        }
        if (message.Text == "Дай мне фото")
        {
            await UpLoadPhoto(botClient, update, cancellationToken);
        }
    }
    private static async Task AdminMessageHeandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        if (message.Text == "/start")
        {
            await botClient.SendTextMessageAsync(
             chatId: chatId,
             text: "Команды:\n /DeleteReply - удаляет Reply кнопки " +
             "\n /NextWave - переходит на следующую волну \nВолна это один поток абитуриентов" +
             "\n /SelectUserOnLastWave - выводит данные последней волны" +
             "\n /SelectUserWave(НомерВолны) - выводит данные выбранной волны",
             cancellationToken: cancellationToken);

            await botClient.SendTextMessageAsync(
               chatId: chatId,
               text: "Вам будет приходить сообщение при добавлении данных у нового пользователя" +
               "\n\nДля добаления данных нового пользователя напишите pc<номерНоутбука> \n(например pc10) в сообщении с фотографией",
               cancellationToken: cancellationToken);
        }
        if (message.Text == "/DeleteReply")
        {
            await botClient.SendTextMessageAsync(
               chatId: chatId,
               text: "Removing keyboard",
               replyMarkup: new ReplyKeyboardRemove(),
               cancellationToken: cancellationToken);
        }
        if (message.Text == "/NextWave")
        {
            TelegramBotTestContext testContext = new TelegramBotTestContext();
            int wave = testContext.Waves.OrderByDescending(x => x.Id).First().Id + 1;
            testContext.Waves.Add(new Wave(wave));
            await testContext.SaveChangesAsync();

            await AdminSendingWave(botClient, update, cancellationToken, wave);
        }
        if (message.Text == "/SelectUserOnLastWave")
        {
            TelegramBotTestContext testContext = new TelegramBotTestContext();
            string text = $"IDTel |\t\tPC |\tImage |\n";
            int wave = testContext.Waves.OrderByDescending(x => x.Id).First().Id;
            foreach (var item in testContext.Users.Where(x => x.Wave == wave))
            {
                string image = item.Image != null ? "да" : "нет";
                text += $"{item.Idtelegram} |\t{item.NubmerPc} |\t{image} |\n";
            }
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                cancellationToken: cancellationToken);
        }
        if (message.Text!.Contains("/SelectUserWave"))
        {
            TelegramBotTestContext testContext = new TelegramBotTestContext();
            int wave = 0;
            try
            {
                wave = testContext.Waves.First(x => x.Id == int.Parse(message.Text.Replace("/SelectUserWave", ""))).Id;
            }
            catch
            {
                await botClient.SendTextMessageAsync(
                       chatId: chatId,
                       text: "Волна не найдена",
                       cancellationToken: cancellationToken);
                return;
            }
            string text = $"IDTel |\t\tPC |\tImage |\n";
            foreach (var item in testContext.Users.Where(x => x.Wave == wave))
            {
                string image = item.Image != null ? "да" : "нет";
                text += $"{item.Idtelegram} |\t{item.NubmerPc} |\t{image} |\n";
            }
            await botClient.SendTextMessageAsync(
               chatId: chatId,
               text: text,
               cancellationToken: cancellationToken);
        }
    }
    private static async Task UpLoadPhoto(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update.Message!.Chat.Id;

        TelegramBotTestContext testContext = new TelegramBotTestContext();
        ConsoleAppTelegramBot.Models.User user = testContext.Users.FirstOrDefault(x => x.Idtelegram == chatId)!;
        if (user != null)
        {
            if (user.Image == null)
            {
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Фото ещё не загружено!",
                cancellationToken: cancellationToken);
                return;
            }
            Stream stream = new MemoryStream(user.Image);

            await botClient.SendPhotoAsync(
            chatId: chatId,
            photo: InputFile.FromStream(stream),
            caption: @$"<b> Вот Ваша фотография {user.FullName} </b>",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
        }
    }
    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}