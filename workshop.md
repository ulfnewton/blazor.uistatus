# Workshop: Bygg vidare på code‑along – robust UI‑status i Blazor Server

**Tid:** resterande pass 3/2  
**Förkunskap:** Ni har just gjort **code‑along** där vi skapade en gemensam **UI‑statuskälla** (`IUiStatus`/`UiStatus`), visade **Busy / Error / Offline (Reconnecting)** från **MainLayout**, och introducerade ett första **operationsmönster** för att köra åtgärder på ett konsekvent sätt.

> **Mål för workshopen:**
>
> 1.  Förvandla code‑along‑resultatet till ett **återanvändbart mönster** (`IUiOperations`)
> 2.  Använd mönstret i en **riktig sida**: *Ladda (GET‑liknande), Skapa, Ta bort*, samt *Offline‑puls*
> 3.  Säkerställ **Cancel** (avbryt) och lagom **disable** av knappar under pågående åtgärd
> 4.  Lämna ifrån er en **liten, körbar feature** med konsekventa statusytor i **layouten**

***

## 1) Vad ska applikationen göra? (funktionell beskrivning)

Ni bygger (eller vidareutvecklar) en **Workshop‑sida** med följande beteenden:

1.  **Ladda lista**
    *   När användaren klickar **Ladda om**:
        *   **Busy** visas **omedelbart** (global overlay från `MainLayout`).
        *   Efter ca **1 s** ”kommer data” (dummydata räcker).
        *   Under väntan är **knappar disable**.
        *   Användaren kan **Avbryta**; då ska **ingen Error** visas.

2.  **Skapa**
    *   Skapar ett nytt item efter kort väntan.
    *   **Busy** visas under väntan; knappar är disable.
    *   Vid lyckat resultat visas uppdaterad lista; **ingen modal** behövs.

3.  **Ta bort**
    *   Tar bort det valda itemet efter kort väntan.
    *   **Ibland** ska det **misslyckas** (t.ex. 1 av 3).
    *   Vid fel visas **Error‑panel** (global, från layout) med **kort rubrik + kort förklaring**.
    *   Vid lyckat resultat uppdateras listan.

4.  **Offline / Reconnecting**
    *   En knapp **Simulera återanslutning (2 s)** som visar en **tunn banner** längst upp.
    *   Resten av sidan ska vara **oförändrad** (ingen popup, ingen blockering).

> **Viktigt:** Ingenting av **Busy / Error / Offline** får implementeras inne i sidans komponent som lokala spinners/alerts. **Allt** redovisas i **MainLayout** via den gemensamma statuskällan (`IUiStatus`).

***

## 2) Ramverket ni ska använda i koden

### 2.1 `IUiStatus` (från code‑along)

*   Håller **IsBusy**, **IsOffline**, **Error** och ett **Changed**‑event
*   Anropas från komponenter/tjänster för att trigga layoutens ytor

*(Ni har redan detta – återanvänd.)*

### 2.2 **Nytt**: `IUiOperations` – ett tydligt operationsmönster

Syfte: alla åtgärder (ladda, skapa, ta bort) ska **automatiskt**:

*   slå på **Busy** omedelbart,
*   köra arbetet,
*   **fånga fel** och sätta layoutens **Error** konsekvent (rubrik + kort text),
*   slå av **Busy** i `finally`.

Ni får gärna utgå från denna **referensdefinition** och anpassa:

```csharp
public interface IUiOperations
{
    /// <summary>
    /// Kör en UI-åtgärd med konsekvent Busy/Error-hantering.
    /// Avbryt via CancellationToken (t.ex. vid Cancel-knapp).
    /// </summary>
    Task RunAsync(Func<CancellationToken, Task> work, CancellationToken ct = default);

    /// <summary>
    /// Variant som returnerar ett värde.
    /// </summary>
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default);
}
```

**Minimikrav på er implementation (`UiOperations`)**:

*   Sätt **Busy(true)** före körning; **Busy(false)** i `finally`.
*   Fånga **OperationCanceledException** **tyst** (Cancel ska inte ge felpanel).
*   Fånga övriga fel och sätt `UiStatus.SetError(new UiError("Något gick fel", ex.Message))`.
*   (Valfritt men bra) Injicera `ILogger<UiOperations>` och logga kort **vid fel**.

> **OBS:** Låt `IUiOperations` **inte** känna till *vilken* sida som ropade – den ska enbart prata med `IUiStatus` och sköta mönstret. På så vis blir den **återanvändbar** i hela appen.

***

## 3) Steg‑för‑steg (instruktioner)

### Steg 0 — Starta/återanvänd projektet

*   Antingen `dotnet new blazorserver -n Workshop` och lägg in ert code‑along‑material,
*   eller fortsätt i befintligt repo/solution från code‑along.

### Steg 1 — Skapa `IUiOperations` + `UiOperations`

1.  Skapa mapp **`Services`** (om den inte finns).
2.  Lägg in interfacet **`IUiOperations`** (som ovan) samt implementering **`UiOperations`**.
3.  Registrera i **`Program.cs`**:
    ```csharp
    builder.Services.AddScoped<IUiOperations, UiOperations>();
    ```
4.  Verifiera att projektet bygger.

### Steg 2 — Koppla en **Workshop‑sida**

1.  Skapa **`/Pages/Workshop.razor`** med en enkel lista och knappar: *Ladda om*, *Skapa*, *Ta bort*, *Offline‑puls*, *Avbryt*.
2.  **Injicera** `IUiStatus` och `IUiOperations`.
3.  Implementera knapparna så att **alla** åtgärder körs via:
    ```csharp
    await UiOps.RunAsync(async ct => { /* Task.Delay + logik */ }, ct);
    ```
4.  Lägg till **Cancel** för *Ladda om* med `CancellationTokenSource`:
    *   Skapa `CTS` på klick, länka med `ct`, `await Task.Delay(…)` med `linked.Token`.
    *   Cancel‑knapp anropar `cts.Cancel()` → **Inget Error** ska visas.

### Steg 3 — Säkerställ layoutbeteenden

1.  **MainLayout** visar redan **Busy / Error / Offline**.
2.  Blockera dubbelklick: disable knappar **under** väntan (t.ex. styr med `IsBusy` från status).
3.  **Empty‑state**: om listan är tom, visa ett enkelt meddelande i sidan (inte en felpanel).

### Steg 4 — Testa scenarierna

*   **Ladda om** → Busy → data efter \~1 s → Avbryt ska inte ge Error.
*   **Skapa** → Busy → ny rad dyker upp.
*   **Ta bort** → lyckas oftast, **ibland** fel → Error‑panel med kort text.
*   **Offline‑puls** → tunn banner i toppen, UI i övrigt oförändrat.

***

## 4) Acceptanskriterier (Godkänt)

*   [ ] **ALLA åtgärder** i er Workshop‑sida går via **`IUiOperations.RunAsync(...)`** (och ev. generiska varianten).
*   [ ] **Busy** visas omedelbart och släcks alltid (även vid fel/el. Cancel).
*   [ ] **Error** visas **endast** när åtgärd **faktiskt** fallerar (inte vid Cancel).
*   [ ] **Offline‑banner** fungerar; resten av sidan fortsätter visas.
*   [ ] Knappar är **disable** under väntan; **Empty‑state** syns när listan är tom.

***

## 5) Extraövningar (för den som hinner)

1.  **Optimistisk uppdatering**: Lägg till ett item direkt (optimistiskt) och **rulla tillbaka** om `RunAsync` kastar fel.
2.  **Timeout‑policy**: Om en åtgärd tar > 2 s, ändra Busy‑overlayn diskret (t.ex. “Det tar längre tid än väntat …”).
3.  **Felrubriker**: Variera `UiError.Title` beroende på feltyp (förberedelse inför torsdagens 201/400/404/409/204).
4.  **Global Reset**: En knapp i layout som **rensar Error** och stänger **Busy/Offline** om de råkat hänga.

***

## 6) Inlämning/visning (klassrummet innan 16:00)

*   Visa **Workshop‑sidan** live:
    1.  **Ladda om** → Visa Busy → **Avbryt** (ingen Error)
    2.  **Skapa** → Rad läggs till
    3.  **Ta bort** → Visa att Error ibland visas **konsekvent**
    4.  **Offline‑puls** → Banner överst, UI orört
*   Peka kort i koden:
    *   `IUiOperations` (interface)
    *   `UiOperations` (implementering)
    *   **MainLayout** (statusytor)
    *   Workshop‑knapparnas anrop till `RunAsync`

> **Tips:** Lägg gärna tre **”om–så”**‑regler som kommentarer överst i `Workshop.razor`:
>
> *   *Om väntan startar →* visa Busy i layout + disable knappar
> *   *Om avbrutet →* inga fel, bara släck Busy
> *   *Om fel →* en kort Error‑panel (rubrik + detalj)
