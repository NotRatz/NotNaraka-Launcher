# 🧪 Quick Test Guide - Game Download Feature

## How to Test the New Feature

### Prerequisites
1. Build the launcher using: `.\build-launcher.ps1` or `.\build-multifile.ps1`
2. Run the launcher: `NarakaTweaks.Launcher.exe`

---

## Test Scenario 1: Automatic Download & Install

**Steps:**
1. Launch the application
2. Navigate to "📥 Clients" tab
3. Click on either:
   - "Download CN Client" button (Chinese version)
   - "Download Global Client" button (Global version)

4. Wait for URL resolution (3-5 seconds)

5. When prompted with download options:
   - Click **"NO"** for automatic download via launcher

6. **Folder Selection Dialog** appears:
   - Select or create a folder (e.g., `C:\Games\Naraka-Test`)
   - Click OK

7. **Confirmation Dialog** appears showing:
   - Installation folder path
   - Download URL
   - Excluded folders notice
   - Click **"YES"** to continue

8. **Progress Window** appears showing:
   - Download progress (0-100%)
   - MB downloaded / Total MB
   - Extraction progress
   - Current file being extracted

9. **Success Dialog** appears showing:
   - Files extracted count
   - Time taken
   - Installation location

---

## Test Scenario 2: Manual Browser Download

**Steps:**
1. Launch the application
2. Navigate to "📥 Clients" tab
3. Click on a download button
4. Click **"YES"** for browser download
5. Browser opens with download page

---

## Test Scenario 3: Cancellation Tests

### Cancel at Folder Selection
1. Start download flow
2. Click **"Cancel"** on folder selection dialog
3. ✅ Should return to launcher without errors

### Cancel at Confirmation
1. Start download flow
2. Select folder
3. Click **"NO"** on confirmation dialog
4. ✅ Should return to launcher without errors

---

## Verification Checklist

After installation completes, verify:

- [ ] Installation folder contains game files
- [ ] `Bin` folder is **NOT** present
- [ ] `netease.mpay.webviewsupport.cef90440` folder is **NOT** present
- [ ] Game executable is present (e.g., `NarakaBladepoint.exe`)
- [ ] No `Naraka/program/` prefix in folder structure
- [ ] Files are in the root of selected folder

---

## Expected File Structure

**Selected Folder:** `C:\Games\Naraka-Test`

**After Installation (Expected):**
```
C:\Games\Naraka-Test\
├── NarakaBladepoint.exe         ✅ Present
├── NarakaBladepoint_Data\       ✅ Present
├── Config\                      ✅ Present
├── (other game folders)         ✅ Present
├── Bin\                         ❌ Should NOT be present
└── netease.mpay...              ❌ Should NOT be present
```

---

## Known Limitations

1. **No .NET SDK on this machine** - Build will fail
   - Solution: Install .NET 8 SDK or use a machine that has it

2. **Large Download Size** - CN/Global clients are 2-4 GB
   - Ensure stable internet connection
   - First test might take 30-60 minutes

3. **Download URL Resolution** - May fail if websites change structure
   - Fallback: Browser download option always available

---

## Troubleshooting

### "Could not resolve download URL"
- **Cause:** Website structure changed or network issue
- **Solution:** Use browser download option (click "YES")

### "Error selecting folder"
- **Cause:** Missing Windows Forms reference
- **Solution:** Ensure `System.Windows.Forms` is referenced in project

### "Extraction failed"
- **Cause:** Corrupted download or disk space issue
- **Solution:** Check disk space, retry download

### Progress window freezes
- **Cause:** Large file extraction can appear frozen
- **Solution:** Wait patiently, check Task Manager for activity

---

## Performance Notes

### Expected Times (on typical connection):

**Download Phase:**
- CN Client (2.5 GB): ~20-40 minutes on 10 Mbps
- Global Client (3 GB): ~25-50 minutes on 10 Mbps

**Extraction Phase:**
- ~5-10 minutes (depends on disk speed)
- SSD: 2-5 minutes
- HDD: 5-10 minutes

**Total Time:** 30-60 minutes for complete installation

---

## Success Indicators

✅ **Download Success:**
- Progress reaches 100%
- "Download complete!" message appears
- No error dialogs

✅ **Extraction Success:**
- Files extracted count > 0
- Success dialog shows statistics
- Installation folder contains game files

✅ **Overall Success:**
- No errors during entire process
- Game files present in chosen folder
- Excluded folders not present
- Ready to launch game

---

## Debug Information

If errors occur, check:

1. **Application Logs** (if implemented)
2. **Temp Folder:** `%TEMP%\Naraka_Download_*`
   - Should be cleaned up automatically
   - If present after error, delete manually

3. **Disk Space:**
   - Require ~10 GB free space
   - 3-4 GB for ZIP + 3-4 GB for extracted files

4. **Network:**
   - Check internet connection
   - Try again during off-peak hours

---

## Manual Testing Commands

### Check File Counts
```powershell
# Count files in installation folder
(Get-ChildItem -Path "C:\Games\Naraka-Test" -Recurse -File).Count

# Check if Bin folder exists (should return False)
Test-Path "C:\Games\Naraka-Test\Bin"

# Check if netease folder exists (should return False)
Test-Path "C:\Games\Naraka-Test\netease.mpay.webviewsupport.cef90440"
```

### Check Folder Size
```powershell
# Get installation size
$size = (Get-ChildItem -Path "C:\Games\Naraka-Test" -Recurse | Measure-Object -Property Length -Sum).Sum
"Installation Size: {0:N2} GB" -f ($size / 1GB)
```

---

**Last Updated:** October 12, 2025  
**Feature Status:** ✅ Implementation Complete
