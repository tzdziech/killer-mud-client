using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>
/// Single source of truth for the short help shown both next to panels and in the central Help
/// window. Keeping the copy here prevents docked panels, Terminal overlays and full Help from
/// drifting apart.
/// </summary>
public static class PanelHelpCatalog
{
    public static IReadOnlyList<PanelHelpTopic> All { get; } =
    [
        new(
            "Group",
            "Drużyna",
            "Panel pokazuje członków Twojej drużyny, ich stan zdrowia, poziom ruchu, pozycję oraz aktualne położenie. Przyciski pod postacią używają wybranego zaklęcia lub umiejętności na tej osobie.",
            ["Zielone kropki nad przyciskiem zaklęcia pokazują liczbę jego zapamiętanych użyć."],
            "Przez ⚙ możesz dodawać i usuwać własne przyciski. Podaj pełną nazwę zaklęcia lub umiejętności oraz krótki napis widoczny na przycisku.",
            []),
        new(
            "MemSpells",
            "Mem i Buffy",
            "Górna część pokazuje zaklęcia zapamiętane, wykorzystane i właśnie zapamiętywane. Sekcja Buffy pilnuje efektów z aktualnie wybranego zestawu.",
            [
                "[+] — buff jest aktywny; [-] — buffa brakuje.",
                "[2/1] — 2 użycia są zapamiętane, a 1 zostało wykorzystane.",
                "Kliknięcie buffa rzuca go, jeśli zaklęcie jest dostępne. RZUĆ BRAKUJĄCE rzuca dostępne zaklęcia, których efektów brakuje."
            ],
            "Przez ⚙ możesz tworzyć zestawy, zmieniać ich nazwy, usuwać je, dodawać śledzone zaklęcia i ustawić liczbę kolumn. Pojedynczy buff usuwa się przyciskiem ✕ w panelu.",
            []),
        new(
            "OffensiveActions",
            "Offensywne i Definiowalne",
            "Offensywne to szybkie przyciski zaklęć i umiejętności, używane głównie podczas walki. Definiowalne wykonują wpisane przez Ciebie polecenia.",
            [
                "Zielone kropki pokazują liczbę zapamiętanych użyć zaklęcia.",
                "Czerwone ★ oznaczają, że umiejętność nie jest jeszcze ponownie gotowa."
            ],
            "Przez ⚙ możesz dodawać i usuwać przyciski, ustawić osobną liczbę przycisków w rzędzie oraz ułożyć obie sekcje pionowo albo obok siebie.",
            []),
        new(
            "Map",
            "Ruch",
            "Przyciski N / S / W / E / U / D służą do poruszania postacią. Dostępne wyjścia są aktywne, a pozostałe kierunki pozostają widoczne, lecz są wyłączone.",
            ["🔒 oznacza zamknięte przejście. Kliknij je normalnie, a klient spróbuje je otworzyć."],
            "Ustawienia mapy pod ⚙ nie zmieniają przypisania klawiszy ruchu.",
            [
                "NumPad 8 — północ; 2 — południe; 4 — zachód; 6 — wschód.",
                "NumPad 9 — góra; 3 — dół."
            ])
    ];

    public static PanelHelpTopic? Find(string? panelId) =>
        All.FirstOrDefault(topic => string.Equals(topic.PanelId, panelId, StringComparison.Ordinal));
}
