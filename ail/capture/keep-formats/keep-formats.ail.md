# Keeping every format

A copy is more than its text: a web page's formatting, a file manager's
files, an image, or an app's own data. Pasting an item from the history puts
all of it back.

```ail
node KeepFormats : flow   level: experience
  why   An item pasted from the history must paste exactly as it did when it
        was copied, or the history cannot stand in for copying again.   <- Capture
  what
    Scenario: rich text keeps its HTML
      Given a web page copied "Rich bold and italic" with its formatting as HTML
      And "plain after rich" was copied after it
      When the user picks the rich text from the history and pastes it
      Then the text box shows "Rich bold and italic"
      And the clipboard offers the same HTML, unchanged
    Scenario: copied files are kept as files
      Given a file manager copied the file "report.txt"
      And some text was copied after it
      When the user picks the file from the history and pastes it
      Then the clipboard offers that file as a file again
    Scenario: another app's own format is kept byte for byte
      Given an app copied data in a format only it knows
      And some text was copied after it
      When the user picks that item from the history and pastes it
      Then the clipboard offers that format with exactly the same bytes
    Scenario: an image is kept, and other apps can read it back
      Given an image was copied
      And some text was copied after it
      When the user picks the image from the history and pastes it
      Then any app that asks for the image gets the same pixels, every time it asks
      And Shapeshifter keeps running and capturing copies
  how
    decide text, files and the image are kept in a portable form, and every other format as raw bytes under its own name
      because rich text, HTML, images, files and app-specific formats must be restored exactly (README, "Everything is kept")
    decide a format the portable parts already provide is not also restored from its raw bytes
      because writing both gives the clipboard two competing values for the same format
    decide formats that are part of the clipboard protocol, or handles that are not plain memory, are never kept; nor any single format over 32 MB
      because reading them crashes or yields garbage, and huge blobs make the history slow (ClipboardFormatClassifier)
    decide an image is kept as PNG and its pixels are restored, not its original bytes
      because the toolkit decodes and re-encodes images; the pixels are what the user copied
    decide an image together with one image file in the temporary folder counts as an image, not a file
      because Firefox on Windows copies images that way, and later deletes the file (#606)
    decide files win over text, and text over an image, when deciding what an item is
      because office apps and browsers put a picture of copied text on the clipboard, which should still read as text
  uses      ClipboardFormats
  sources
    README.md, "Everything is kept"
    GitHub issue #606 (Firefox images on Windows)
    commit 8556808 "Fix crash when an app reads an image restored from the history": before it, on Linux an app reading a restored image made Shapeshifter crash (found while driving)
    driven on Linux (Xvfb, openbox) 2026-11-20: the target app's copy-html, xclip with text/uri-list, application/x-ail-capture-check and image/png; read back with xclip -o and compared with ImageMagick compare
    src/Shapeshifter.Desktop/Services/AvaloniaClipboardBackend.cs, ReadCoreAsync and WriteCoreAsync
    src/Shapeshifter.Core/Clipboard/ClipboardSnapshot.cs, Kind
```
