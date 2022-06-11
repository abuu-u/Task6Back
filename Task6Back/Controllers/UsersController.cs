using System.Globalization;
using Bogus;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;

namespace Task6Back.Controllers;

[ApiController]
[Route("task6/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly string[] GermanAlphabet =
    {
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V",
        "W", "X", "Y", "Z", "Ä", "Ö", "Ü", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o",
        "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", " ", "ä", "ö", "ü"
    };

    private static readonly string[] EnglishAlphabet =
    {
        "A", "a", "B", "b", "C", "c", "D", "d", "E", "e", "F", "f", "G", "g", "H", "h", "I", "i", "J", "j", "K", "k",
        "L", "l", "M", "m", "N", "n", "O", "o", "P", "p", "Q", "q", "R", "r", "S", "s", "T", "t", "U", "u", "V", "v",
        "W", "w", "X", "x", "Y", "y", "Z", "z"
    };

    private static readonly string[] PolishAlphabet =
    {
        "A", "a", "B", "b", "C", "c", "Ć", "ć", "D", "d", "E", "e", "F", "f", "G", "g", "H", "h", "I", "i", "J", "j",
        "K", "k", "L", "l", "Ł", "ł", "M", "m", "N", "n", "O", "o", "P", "p", "Q", "q", "R", "r", "S", "s", "Ś", "s",
        "T", "t", "U", "u", "V", "v", "W", "w", "X", "x", "Y", "y", "Ż", "z", "Ź", "ź", "Z", "ż",
    };

    private static readonly string[] SupportedLocales = { "en", "de", "pl", };

    private static readonly string[] Fields = { "FirstName", "MiddleName", "LastName", "Address", "Phone", };

    private static readonly string[] Errors = { "delete", "add", "swap" };

    [HttpGet("csv")]
    public IActionResult GenerateCsv([FromQuery] int seed, string locale, int page, double errorsCount)
    {
        if (!SupportedLocales.Contains(locale))
        {
            return BadRequest("unsupported locale");
        }

        if (page < 1)
        {
            return BadRequest("page should be more than or equal to 1");
        }

        if (errorsCount is < 0 or > 1000)
        {
            return BadRequest("errorsCount should be more than or equal to 0 and less than or equal to 1000");
        }

        var count = page * 10 + 10;

        var users =
            Enumerable
                .Range(0, count)
                .Select(index => CreateFakeUser(locale, index + seed, index, errorsCount))
                .ToArray();

        string csvStr;

        using (var stream = new MemoryStream())
        using (var reader = new StreamReader(stream))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<User>();
            csv.NextRecord();
            foreach (var user in users)
            {
                csv.WriteRecord(user);
                csv.NextRecord();
            }
            
            writer.Flush();
            stream.Position = 0;

            csvStr = reader.ReadToEnd();
        }

        return Ok(new
        {
            csv = csvStr
        });
    }

    [HttpGet]
    public IActionResult GetUsers([FromQuery] int seed, string locale, int page, double errorsCount)
    {
        if (!SupportedLocales.Contains(locale))
        {
            return BadRequest("unsupported locale");
        }

        if (page < 1)
        {
            return BadRequest("page should be more than or equal to 1");
        }

        if (errorsCount is < 0 or > 1000)
        {
            return BadRequest("errorsCount should be more than or equal to 0 and less than or equal to 1000");
        }

        var count = page == 1 ? 20 : 10;

        return Ok(new
        {
            users =
                Enumerable
                    .Range((page == 1 ? page - 1 : page) * count, count)
                    .Select(index => CreateFakeUser(locale, index + seed, index, errorsCount))
                    .ToArray()
        });
    }

    private static User CreateFakeUser(string locale, int seed, int index, double errorsCount)
    {
        var errorsCountInt = (int)errorsCount;
        var finalErrorsCount = new Random(seed).NextDouble() <= errorsCount - errorsCountInt
            ? errorsCountInt + 1
            : errorsCountInt;

        var fieldsErrorCounts = Enumerable.Range(0, Fields.Length).Select(_ => 0).ToArray();

        var errors = Enumerable
            .Range(0, finalErrorsCount)
            .Select(i =>
            {
                var randomFieldIndex = new Random(seed + i).Next(Fields.Length);

                return new
                {
                    fieldName = Fields[randomFieldIndex],
                    errorType = Errors[fieldsErrorCounts[randomFieldIndex]++ % Errors.Length]
                };
            }).ToArray();

        var fakeUser = new Faker<User>(locale.ToString())
                .StrictMode(false)
                .Rules((f, o) =>
                {
                    o.Index = index;
                    o.RandomIdentifier = seed;
                    o.FirstName = f.Name.FirstName();
                    o.MiddleName = f.Name.FirstName();
                    o.LastName = f.Name.LastName();
                    o.Address = new Random(seed).NextDouble() > 0.5
                        ? f.Address.FullAddress()
                        : f.Address.StreetAddress();
                    o.Phone = f.Phone.PhoneNumber().Replace('x', '1');
                }).UseSeed(seed).Generate()
            ;

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            var field = fakeUser.GetType().GetProperty(error.fieldName);
            var fieldValue = field?.GetValue(fakeUser) as string;

            switch (error.errorType)
            {
                case "delete":
                    DeleteRandomChar(ref fieldValue!, seed + i);
                    break;

                case "add":
                    AddRandomChar(ref fieldValue!, seed + i, locale);
                    break;

                case "swap":
                    SwapRandomNearChars(ref fieldValue!, seed + i);
                    break;
            }

            field?.SetValue(fakeUser, fieldValue);
        }


        return fakeUser;
    }

    private static void DeleteRandomChar(ref string str, int seed)
    {
        var charPosition = new Random(seed).Next(str.Length);
        str = str.Remove(charPosition, 1);
    }

    private static void AddRandomChar(ref string str, int seed, string locale)
    {
        var alphabet = locale switch
        {
            "de" => GermanAlphabet,
            "en" => EnglishAlphabet,
            "pl" => PolishAlphabet,
            _ => throw new ArgumentOutOfRangeException(nameof(locale), locale, null)
        };
        str = str.Insert(new Random(seed).Next(str.Length),
            alphabet[new Random(seed).Next(alphabet.Length)]);
    }

    private static void SwapRandomNearChars(ref string str, int seed)
    {
        var charPosition = new Random(seed).Next(str.Length - 1);
        var charAt = str[charPosition];
        var nextCharAt = str[charPosition + 1];
        ReplaceCharAt(ref str, charPosition, nextCharAt);
        ReplaceCharAt(ref str, charPosition + 1, charAt);
    }

    private static void ReplaceCharAt(ref string str, int position, char charToInsert)
    {
        str = str.Remove(position, 1);
        str = str.Insert(position, charToInsert.ToString());
    }
}