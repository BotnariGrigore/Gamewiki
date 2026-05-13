Folder pentru imaginile cover folosite de aplicație

Unde să pui imaginile
- Pune fișierele de imagine (jpg, jpeg, png, webp, gif) în acest folder: `Assets/Images`.
- În baza de date sau în câmpul `CoverImage` folosește calea relativă față de executabil, de ex.: `Assets/Images/eldenring.jpg`.

Cum sunt încărcate imaginile în UI
- Codul UI încearcă să încarce direct valoarea din `CoverImage` (poate fi URL sau cale relativă/absolută).
- Pentru fișiere locale recomandăm să folosești calea relativă în DB (ex. `Assets/Images/foo.jpg`) și să încarci astfel în aplicație:

```csharp
using System.IO;

string coverValue = article.CoverImage; // "Assets/Images/foo.jpg" sau URL
string localFull = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, coverValue);
if (File.Exists(localFull))
{
    pictureBox.Image = Image.FromFile(localFull);
}
else
{
    try { pictureBox.Load(coverValue); } catch { pictureBox.Image = null; }
}
```

Butonul din `Create Game` (sau din editor) copiază automat imaginea aleasă în acest folder și completează câmpul `CoverImage` cu calea relativă.

Recomandări
- Dimensiune recomandată cover: ~1200x675 (16:9) sau 800x450; optimizează imagini pentru web (sub 1MB).
- Evită caractere speciale în numele fișierelor (folosește litere, cifre, `-` sau `_`).

Dacă vrei, pot adăuga copie automată a imaginilor și butoane similare și pentru editarea articolelor.