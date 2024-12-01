# Renamer

This app has 3 roles for manage (normalize) photos and videos made by various cameras:
1. **Rename Sony PlayMemories Home folders from `DD.MM.YYYY` to `YYYY-MM-DD`.**
   - rename only folders from the current folder
   - optional: delete (move to Recycle Bin) duplicated files if they already exists in the new-named folder
2. **Rename photos and videos to `YYYYMMDD_hhmmss` format.**
   - the rename must be applied to the shortcuts of the original footage: Create shortcuts for your footage and put all the shortcuts together in a new folder!
   - rename only shortcut files (`OriginalName - Shortcut.lnk`) from the current folder to `YYYYMMDD_hhmmss - OriginalName.lnk`
   - supported cameras:
      - Samsung Galaxy (photo and video): `YYYYMMDD_hhmmss` → already good
      - Canon EOS (photo): `IMG_XXXX` → _Date Taken_ metadata is used
      - Sony HDR-CX405 (video): `YYYYMMDDhhmmss` → add an underscore
      - Xiaomi Mi Max3 (photo and video):
        - `IMG_YYYYMMDD_hhmmss` or `VID_YYYYMMDD_hhmmss` → remove the prefix
        - Unix-like time, e.g. `17XXXXXXXXXXX` → _Date Taken_ metadata is used for photos, _Media created_ metadata is used for videos
3. **Delete original footage**.
   - only edited photos are kept from current folder and its subfolders
   - a photo is considered edited if it contains the `-` character
   - remove: RAW (`.CR2`, `.ARW`, `.dng`, `.tif`), unedited JPG, video (`.mp4`)

Download the executable: https://drive.google.com/drive/folders/1jRGDC-J8TB_M-BRELUQnkWROjGm3s4oI?usp=sharing
<br><br>
![Screenshot](https://github.com/AndreiVaida/Renamer/blob/master/Resources/Renamer_2024.08.30.png?raw=true "Screenshot")
## Instructions
1. copy _Renamer.exe_ in the folder you want to rename/clean the files
2. click on the desired button
3. wait to finish, even if the window froze
4. the result is displayed at the bottom of the window with green text, and errors are shown in a MessageBox