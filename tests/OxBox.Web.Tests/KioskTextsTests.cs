using System.Collections;
using System.Globalization;
using System.Resources;
using OxBox.Application.Exchange;
using OxBox.Application.Feedback;
using OxBox.Application.Service;
using OxBox.Web.Kiosk;

namespace OxBox.Web.Tests;

public class KioskTextsTests
{
    public static TheoryData<KioskLanguage, KioskScreen> EveryScreenInEveryLanguage()
    {
        var service = new ServiceScreen("Jan Novák", [8, 12], RestockedCount: 1, LastInsertedPositionNumber: 9);
        var data = new TheoryData<KioskLanguage, KioskScreen>();
        foreach (var language in Enum.GetValues<KioskLanguage>())
        {
            data.Add(language, new ReadyScreen());
            data.Add(language, new OutOfServiceScreen());
            data.Add(language, new ExchangeOpenScreen(8, 1));
            data.Add(language, service);
            data.Add(language, new ServiceScreen("Jan Novák", [], RestockedCount: 0, LastInsertedPositionNumber: null));
            data.Add(language, new ServiceFinishedScreen(2, 3));
            foreach (var error in Enum.GetValues<ExchangeError>())
            {
                data.Add(language, new RejectedScreen(error));
            }

            foreach (var error in Enum.GetValues<ServiceError>())
            {
                data.Add(language, new ServiceRejectedScreen(error, service));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryScreenInEveryLanguage))]
    public void For_EveryScreen_HasATitleAndBody(KioskLanguage language, KioskScreen screen)
    {
        var view = KioskView.For(screen, language);

        view.Title.Should().NotBeNullOrWhiteSpace();
        view.Body.Should().NotBeNullOrWhiteSpace();
        view.Title.Should().NotContain("{");
        view.Body.Should().NotContain("{");
    }

    [Fact]
    public void For_ExchangeOpen_ShowsBothSlots()
    {
        var view = KioskView.For(new ExchangeOpenScreen(8, 1), KioskLanguage.English);

        view.Slots.Should().Equal(new KioskSlot("Empty in", 8), new KioskSlot("Full out", 1));
    }

    [Fact]
    public void For_ServiceScreen_ListsTheEmptySlotsAndNamesTheTechnician()
    {
        var view = KioskView.For(new ServiceScreen("Jan Novák", [8, 12], RestockedCount: 1, LastInsertedPositionNumber: 9), KioskLanguage.English);

        view.Body.Should().Contain("8, 12");
        view.Note.Should().Contain("Jan Novák");
        view.Slots.Should().Equal(new KioskSlot("Full in", 9));
    }

    [Fact]
    public void Resources_EnglishHasExactlyTheCzechKeys()
    {
        var manager = new ResourceManager("OxBox.Web.Resources.Kiosk.KioskTexts", typeof(KioskTexts).Assembly);

        var czech = Keys(manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)!);
        var english = Keys(manager.GetResourceSet(CultureInfo.GetCultureInfo("en"), createIfNotExists: true, tryParents: false)!);

        english.Should().BeEquivalentTo(czech);
    }

    [Theory]
    [InlineData(KioskLanguage.Czech, "Schránky jsou otevřené")]
    [InlineData(KioskLanguage.English, "Your slots are open")]
    public void For_SameScreen_IsWordedInTheChosenLanguage(KioskLanguage language, string expectedTitle)
    {
        KioskView.For(new ExchangeOpenScreen(8, 1), language).Title.Should().Be(expectedTitle);
    }

    [Theory]
    [InlineData("http://box/kiosk/BOX-01", KioskLanguage.Czech)]
    [InlineData("http://box/kiosk/BOX-01?lang=en", KioskLanguage.English)]
    [InlineData("http://box/kiosk/BOX-01?lang=EN", KioskLanguage.English)]
    [InlineData("http://box/kiosk/BOX-01?lang=de", KioskLanguage.Czech)]
    public void DefaultFromUrl_ReadsTheLangParameter(string url, KioskLanguage expected)
    {
        KioskLanguages.DefaultFromUrl(url).Should().Be(expected);
    }

    [Theory]
    [InlineData("cs", KioskLanguage.Czech)]
    [InlineData("EN", KioskLanguage.English)]
    [InlineData("de", null)]
    [InlineData(null, null)]
    public void FromCode_KnownAndUnknownCodes(string? code, KioskLanguage? expected)
    {
        KioskLanguages.FromCode(code).Should().Be(expected);
    }

    private static List<string> Keys(ResourceSet set) =>
        set.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToList();
}
