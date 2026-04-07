using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace tg_bot_exam;

public static class Program
{
    private static readonly ITelegramBotClient myBot = new TelegramBotClient("8687611275:AAElK2vl_oZFJqt7b9p4M3URcx_FzFRPo6U"); //@photosaver3bot
    private static readonly string keyOfPhotos = "N1E1J60n5k_A7-CjQVapi4aKIM6CRw7BWOqH9Sx08Do";
    private static readonly string UsersFilePath = "users.txt";
    private static readonly string ImagesFolder = "LocalImages";

    public static async Task Main()
    {
        if (!Directory.Exists(ImagesFolder)) Directory.CreateDirectory(ImagesFolder);

        Console.WriteLine("Bot ishladi");

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        myBot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            HandlePollingErrorAsync,

            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        Console.WriteLine("enter bosing toxtaydi");
        Console.ReadLine();
        await cts.CancelAsync();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } messageText } message) return;

        var chatId = message.Chat.Id;

        if (messageText == "/start")
        {
            var userInfo = $"{DateTime.Now}: ID {chatId}, Name {message.Chat.FirstName}";
            await File.AppendAllLinesAsync(UsersFilePath, [userInfo], ct);

            await botClient.SendMessage(chatId, "Assalomu Alaykum! Rasm qidirish uchun sozni inglizchada yozing", cancellationToken: ct);
            return;
        }

        await botClient.SendMessage(chatId, $"{messageText} qidirilmoqda...", cancellationToken: ct);
        await DownloadAndSendImages(chatId, messageText, ct);
    }

    private static async Task DownloadAndSendImages(long chatId, string query, CancellationToken ct)
    {
        using var client = new HttpClient();
        var apiUrl = $"https://api.unsplash.com/search/photos?query={query}&per_page=3&client_id={keyOfPhotos}";

        try
        {
            var response = await client.GetStringAsync(apiUrl, ct);
            var json = JObject.Parse(response);
            var results = json["results"];

            if (results == null || !results.Any())
            {
                await myBot.SendMessage(chatId, "rasm topilmadi", cancellationToken: ct);
                return;
            }

            int index = 1;
            foreach (var item in results)
            {
                var imageUrl = item["urls"]?["regular"]?.ToString();
                if (string.IsNullOrEmpty(imageUrl)) continue;

                var fileName = Path.Combine(ImagesFolder, $"{query}_{index}_{DateTime.Now.Ticks}.jpg");
                var imageBytes = await client.GetByteArrayAsync(imageUrl, ct);
                await File.WriteAllBytesAsync(fileName, imageBytes, ct);

                await using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                await myBot.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromStream(stream),
                    caption: $"Rasm {index}",
                    cancellationToken: ct);

                index++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Xato yuz berdi: {ex.Message}");
            await myBot.SendMessage(chatId, "Rasmlarni yuklashda xatolik yuz berdi.", cancellationToken: ct);
        }
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        Console.WriteLine($"Telegram xatosi: {exception.Message}");
        return Task.CompletedTask;
    }
}