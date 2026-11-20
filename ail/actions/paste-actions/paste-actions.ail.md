# Pasting in another shape

Paste puts the item back exactly as it was copied. Paste as plain text drops
its formatting, and Paste as file hands it over as a file.

```ail
node PasteActions : flow   level: experience
  why   The app being pasted into decides what it does with the clipboard, so
        the user needs a way to give it only the text, or a file instead of
        text, when the original shape does not fit (#6, #158).   <- Actions
  what
    Scenario: Paste puts back every format
      Given "rich" was copied with formatting, and then "plain"
      When the user holds Ctrl+V until the picker appears, taps Down and lets go of Ctrl
      Then "rich" is pasted
      And the clipboard holds "rich" both as plain text and with its formatting, as when it was copied
    Scenario: Paste as plain text leaves the formatting out
      Given "styled" was copied with formatting
      When the user opens its actions, chooses "Paste as plain text" and lets go of Ctrl
      Then "styled" is pasted
      And the clipboard holds only the plain text "styled"
    Scenario: Paste as file turns text into a .txt file
      Given a text of two lines was copied
      When the user opens its actions, chooses "Paste as file" and lets go of Ctrl
      Then the clipboard holds one file, "Text <date> <time>.txt", with exactly that text
      And a text box, which takes no files, receives nothing
    Scenario: Paste as file turns an image into an image file
      Given a PNG image was copied
      When the user opens its actions, chooses "Paste as file" and lets go of Ctrl
      Then the clipboard holds one file, "Image <date> <time>.png", showing that image at its size
  how
    decide Paste restores everything that was copied, every format the source app offered
      because pasting from the history must give the same result as pasting right after copying; once all text came out plain (#168)
    decide Paste moves the item to the top of the history; the other paste actions do not
      because "unknown, confirm with the owner: should Paste as plain text or as file also move it up?"
    decide Paste as plain text is offered only for text that has formatting; plain text from formatted text keeps its own plain version, or is taken out of the web page format when there is none
      because "unknown, confirm with the owner: because it would paste the same as Paste?"
    decide what Paste as plain text and Paste as file put on the clipboard is not added to the history
      because "unknown, confirm with the owner: because the user did not copy it?"
    decide Paste as file names the file after when the item was copied, "Text" or "Image" then the date and time, and gives each item a folder of its own
      because items copied in the same second must not overwrite each other's file
    decide an image file gets the image's own type: PNG, JPEG, GIF, BMP, WebP or TIFF, and PNG when it is none of those
      because the file must open as what it is (PR #629)
    decide the files are temporary and deleted the next time Shapeshifter starts
      because "unknown, confirm with the owner"
    decide on Linux the file is offered only as a list of file addresses
      because "unknown, confirm with the owner: file managers that want their own format cannot paste it"
  uses      ActionCatalog
  sources
    GitHub issues #6, #158, #168; PRs #626, #629
    commits a2ef2aa "Rename Copy as file to Paste as file and make it paste", 6092ede "Offer Paste as file for images"
    driven on Linux (Xvfb, openbox) 2026-11-20 with xdotool and xclip: the clipboard's formats after each action, the files written, a text box receiving nothing from Paste as file
    src/Shapeshifter.Core/Actions/PasteActions.cs; src/Shapeshifter.Core/Actions/FileActions.cs, PasteAsFileAction
    src/Shapeshifter.Core/Actions/IClipboardAction.cs, ActionContext.WriteAndPasteAsync
    src/Shapeshifter.Desktop/App.axaml.cs, CleanTemporaryFiles
```
