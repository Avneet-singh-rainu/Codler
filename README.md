# Codler

A Visual Studio extension that makes **your own** C# methods stand out from framework calls.

In a dense file, a call to `CalculateInvoiceTotal()` looks exactly like a call to `ToString()`.
Codler uses Roslyn to work out which methods actually have source in your solution, and gives
them their own colour — so you can see at a glance what is your code and what is the platform's.

> **Status: preview (v1.2).** Working, but rough. See [Known limitations](#known-limitations).

---

## Requirements

| | |
|---|---|
| Visual Studio | 2022 17.0 or later (Community, Professional, Enterprise) |
| Architecture | amd64 |
| Workload | Visual Studio extension development |
| Target framework | .NET Framework 4.8 |

---

## Getting started

### Build and debug

```bash
git clone https://github.com/Avneet-singh-rainu/Codler.git
cd Codler
```

Open `Codler.slnx` in Visual Studio and press <kbd>F5</kbd>. This launches the
**Experimental Instance** of Visual Studio with Codler loaded, leaving your normal
installation untouched. Open any C# file in that instance to see the highlighting.

### Install the built extension

After a Release build, install `Codler\bin\Release\Codler.vsix` by double-clicking it,
then restart Visual Studio.

---

## Configuration

**Tools → Options → Codler → User Methods**

| Setting | Category | Default | Effect |
|---|---|---|---|
| Enable User Method Highlighting | General | `true` | Master on/off switch |
| Definition Foreground Color | Colors — Method Definitions | `LightGreen` | Colour for where a method is *declared* |
| Definition Opacity Percent | Colors — Method Definitions | — | Opacity for declarations |
| Invocation Foreground Color | Colors — Method Invocations | — | Colour for where a method is *called* |
| Invocation Opacity Percent | Colors — Method Invocations | — | Opacity for calls |
| Font Size | Font | `16` | *Not currently applied* |
| Font Family | Font | `Consolas` | *Not currently applied* |
| Bold | Font | `true` | *Not currently applied* |
| Italic | Font | `false` | *Not currently applied* |

Colour changes currently require restarting Visual Studio to take effect.

---

## What gets highlighted

**As definitions:**

- Method declarations
- Constructor declarations
- Property declarations
- Interface method declarations

**As invocations:**

- Direct calls — `DoWork()`
- Member access calls — `service.DoWork()`, `Helper.DoWork()`
- Generic calls — `Convert<T>()`
- `new MyClass()` where the constructor is yours

A method counts as *yours* when its symbol resolves to source inside the solution and its
namespace is not a known framework namespace. Methods from `System.*`, `Microsoft.AspNetCore.*`,
`Microsoft.EntityFrameworkCore.*` and similar are deliberately left alone.

---

## How it works

Three MEF components, wired together by the editor:

```
UserMethodTaggerProvider        IViewTaggerProvider, [ContentType("CSharp")]
        │                       creates one tagger per C# view
        ▼
UserMethodTagger                parses the buffer with Roslyn, builds a compilation
        │                       including sibling project sources, resolves each
        │                       invocation to a symbol, emits UserMethodHighlightTag
        ▼
UserMethodAdornmentFactory      IWpfTextViewCreationListener
        │                       aggregates the tags via IViewTagAggregatorFactoryService
        ▼
UserMethodAdornmentLayer        draws the coloured adornments over the text
```

Cross-project resolution works by reading the open file's `.csproj`, following its
`<ProjectReference>` entries, and parsing the `.cs` files it finds into a shared syntax-tree
cache.

---

## Project structure

```
Codler/
├── CodlerPackage.cs                     AsyncPackage entry point, registers the options page
├── CodlerOptionsPage.cs                 Tools → Options settings
├── UserMethodTaggerProvider.cs          MEF export, creates the tagger
├── UserMethodRoslynTagger.cs            Roslyn analysis and tagging
├── UserMethodHighlightTag.cs            The tag type (definition vs invocation)
├── UserMethodAdornmentFactory.cs        Renders adornments from tags
├── UserMethodAdornmentLayer.cs          Adornment layer definition
├── CodlerMethodClassificationFormat.cs  Classification formats (currently unused)
├── VSCommandTable.vsct                  Command table
└── source.extension.vsixmanifest        Extension manifest
```

---

## Known limitations

- **Performance on large files.** Analysis re-runs on every keystroke and re-renders on every
  scroll, which is noticeable in files of a few thousand lines.
- **Colour changes need a restart.** Options are read once when the editor components are created.
- **Font settings do nothing yet.** The renderer only applies colour and opacity.
- **Cross-project support is one level deep.** Direct `<ProjectReference>` entries are followed;
  their own references are not.
- **`.csproj` parsing is regex-based**, so unusual project layouts may not resolve.
- **Common method names may be skipped.** A name-based exclusion list suppresses highlighting for
  names like `Create`, `Save` and `Validate` when a symbol cannot be resolved, so some of your own
  methods with those names may not light up.

---

## Roadmap

- Use the Visual Studio Roslyn workspace instead of hand-building a compilation
- Incremental, off-UI-thread analysis
- Live options updates without restarting
- Apply the font settings
- Solution-wide project graph rather than one-level references

---

## License

Not yet specified.
