# Gravity Painter — Android Optimization Guide
### Fix: 1 GB Build Size + 15 FPS Performance

> **Diagnosis date:** July 2026 — based on live file audit of your project

---

## 🔍 Root Cause Diagnosis

Your 1 GB build has **three main culprits** — here is the real breakdown:

| Cause | Disk Size | Impact |
|-------|-----------|--------|
| **GLB models** (oversized for mobile) | **405 MB** | 🔴 Build size |
| **Sci-Fi Modular Pack textures** (2K–4K PBR, 158 files) | **298 MB** | 🔴 Build size + VRAM |
| **UI sprite PNGs** (uncompressed, up to 5.7 MB each) | ~35 MB | 🟡 Build size |
| **Main menu MP4 video** | **13 MB** | 🟡 Build size |
| **Unused ThirdParty assets** included in build | variable | 🔴 Build size |
| **Real-time PBR lighting** on a mobile game | — | 🔴 FPS killer |
| **No object pooling** for tiles | — | 🟡 FPS |

---

## 🚨 Fix #1 — GLB Model Sizes (BIGGEST WIN)

Your GLB files are enormous for a mobile game. These are the actual sizes on disk:

| File | Size | Problem |
|------|------|---------|
| `tiles.glb` | **63 MB** | Main tile — spawned many times at runtime |
| `Finish_Line.glb` | **37 MB** | One per level |
| `Ai_Nova_Blue/Red/White/Yellow.glb` | **~40 MB each** | Ball skin variants |
| `coins.glb` | **32 MB** | Spawned many times per level |
| `Magnet.glb` | **28 MB** | Power-up |
| `LaserGate.glb` | **22 MB** | Obstacle |

**The problem:** These GLB files likely contain **embedded 4K textures** baked in. On Android, a 40 MB GLB can expand to 200+ MB in GPU memory.

### Fix steps:
1. **Open each GLB in Blender** (free) → File → Import → glTF 2.0
2. In the **Material Properties**, check what textures are embedded
3. Reduce embedded textures to **512×512** or **1024×1024** max for mobile
4. Re-export with **Draco compression** enabled (`File → Export → glTF 2.0 → Geometry → Compression: Draco`)
5. Target size: `tiles.glb` should be under **5 MB**, ball models under **3 MB**

**Alternative quick fix in Unity:** In Project panel, select each GLB → Inspector → `Model` tab → check **"Mesh Compression"** = High, and **"Optimize Mesh"** = checked.

---

## 🚨 Fix #2 — Sci-Fi Modular Pack Textures (298 MB!)

You have **158 texture files totalling 298 MB** from the Sci-Fi Modular Pack. These are full PBR maps (Albedo, Normal, Metallic, AO) at 2K–4K resolution.

**The problem:** Unity will include ALL textures from imported Asset Store packs even if you only use 3 of the 29 mesh types.

### Fix steps:

#### Step A — Strip unused textures
1. In Unity, open **Edit → Project Settings → Editor → Asset Serialization**
2. Use **Window → Asset Management → Addressables** or just go to **Edit → Build → Clean Build**
3. Better: run **Window → Asset Management → Asset Usage Viewer** to see which Sci-Fi assets are actually referenced by your scenes

#### Step B — Reduce texture resolution for mobile
1. Select ALL textures in `Assets/ThirdParty/Sci Fi Modular Pack/Textures/`
2. In Inspector → **Texture Import Settings**:
   - Max Size: change from `2048` → **`512`** for mobile (tiles are small on screen)
   - Format: **`ASTC 6x6 blocks`** (best quality/size for Android)
   - Override for Android: enable → set Max Size = `512`
3. Apply and re-import

#### Step C — Delete unused Sci-Fi assets entirely
If your game only uses tiles and walls, delete the mesh/texture folders you don't use (Doors, Windows, Lights, Boxes etc.). They add to build if they have any indirect reference.

---

## 🚨 Fix #3 — UI Sprites (Uncompressed PNGs)

Your UI button images are massive:

| File | Size |
|------|------|
| `nextlevel.png` (duplicated!) | 4.9 MB × 2 |
| `home.png` (duplicated!) | 4.8 MB × 2 |
| `restart.png` (duplicated!) | 4.1 MB × 2 |
| `GravityPainterIntroPage.png` | 4.2 MB |
| `FrontPage_Game.png` | 2.2 MB |

**Also:** `nextlevel.png` and `home.png` exist in BOTH `Art/Sprites/UI/` AND `Resources/UI/LevelCompleteUI/` — that's **double the size** shipped in the build!

### Fix steps:
1. **Remove duplicates** — delete from `Resources/UI/LevelCompleteUI/` and load from `Art/Sprites/UI/` only (or vice versa)
2. **Resize PNGs before importing** — button icons don't need to be more than 256×256 or 512×512 pixels. Use any image editor (Preview on Mac works: Tools → Adjust Size)
3. **Set texture import format:**
   - Select all UI sprites → Inspector → Texture Type: `Sprite (2D and UI)`
   - Format: **`ETC2 RGBA8`** or **`ASTC 6x6`** for Android
   - Max Size: **`512`** for icons, **`1024`** for full-screen backgrounds

---

## 🚨 Fix #4 — Enable Unity's Android Build Optimization Settings

These settings are OFF by default and **must be turned on** before building:

### In Unity: Edit → Project Settings → Player → Android

| Setting | Change to | Why |
|---------|-----------|-----|
| **Texture Compression** | `ASTC` | Best compression for modern Android |
| **Scripting Backend** | `IL2CPP` | 30-40% faster than Mono |
| **API Compatibility Level** | `.NET Standard 2.1` | Smaller build |
| **Managed Stripping Level** | `High` | Removes unused C# code |
| **Target Architecture** | `ARM64` only (uncheck ARMv7) | Removes 32-bit bloat |

### In Unity: Edit → Project Settings → Player → Other Settings

| Setting | Change to |
|---------|-----------|
| **Strip Engine Code** | ✅ ON |
| **Optimize Mesh Data** | ✅ ON |
| **Enable Frame Timing Stats** | ❌ OFF |

### In Unity: File → Build Settings → Android

| Setting | Change to |
|---------|-----------|
| **Compression Method** | `LZ4HC` |
| **Build App Bundle (Google Play)** | ✅ ON (AAB instead of APK — Google Play delivers only needed textures) |

---

## 🔴 Fix #5 — FPS: Lighting & Rendering

**15 FPS on Android = real-time lighting is the main killer.**

### Step A — Bake your lighting
1. Go to **Window → Rendering → Lighting**
2. Set **Lighting Mode** to `Baked Indirect`
3. On all your level scene lights: change `Mode` from **Realtime** → **Baked**
4. Click **Generate Lighting** (bottom of Lighting window)
5. This bakes shadows into textures — zero runtime cost

### Step B — Disable shadows entirely (fastest fix)
1. **Edit → Project Settings → Quality** → create an `Android` quality level
2. Set **Shadows** = `Disable`
3. Set **Shadow Distance** = `0`
4. Set **Pixel Light Count** = `1` or `0`

### Step C — Reduce post-processing
1. Find any **Volume** or **Post Process** objects in your scenes
2. Disable: **Bloom, SSAO, Depth of Field, Motion Blur** — these alone can cost 15+ FPS on mobile
3. Keep only: **Color Grading** if needed (cheap)

### Step D — Use URP Mobile settings
1. Find your URP Renderer asset in `Assets/Settings/`
2. Set **Anti-aliasing** = `None` or `FXAA` (not MSAA)
3. Disable **HDR** rendering
4. Set **Render Scale** = `0.85` (slightly lower internal resolution, barely visible, big FPS boost)

---

## 🟡 Fix #6 — Tile Object Pooling (Runtime Performance)

Your `ProceduralLevelBuilder` currently `Instantiate()`s every tile at level start and destroys them all on rebuild. On Android this causes **GC spikes and stutter**.

> **Note:** This is a code change — skip this initially and revisit after the above fixes.

The plan is to implement a simple `ObjectPool<GameObject>` for tiles, coins, and power-ups so they are recycled not destroyed. Unity 2021+ has `UnityEngine.Pool.ObjectPool<T>` built-in.

---

## 🟡 Fix #7 — Remove the Sci-Fi Modular Pack from Build (if unused in scenes)

If the Sci-Fi pack's models/prefabs aren't actually placed in any scene, Unity might still include their textures if they're referenced anywhere. 

1. In Unity: **Window → Analysis → Build Report** (after a build) — check what's actually included
2. If Sci-Fi Modular Pack items appear but you don't use them → move the whole folder out of `Assets/` (into a safe backup folder outside the project)

---

## 📋 Priority Order — Do These First

| Priority | Action | Expected Saving |
|----------|--------|-----------------|
| 🔴 1 | Enable `IL2CPP`, `ARM64 only`, `Managed Stripping High`, `LZ4HC`, build as `.aab` | ~200 MB build size |
| 🔴 2 | Set all SciFi textures → Max 512px, ASTC format in Unity Inspector | ~100 MB + big FPS gain |
| 🔴 3 | Disable/bake lighting, disable shadows on Android quality level | +15–25 FPS |
| 🔴 4 | Remove duplicate UI sprites (nextlevel/home/restart in two folders) | ~15 MB |
| 🟡 5 | Compress GLB files via Draco or reduce embedded textures | ~200–300 MB |
| 🟡 6 | Disable SSAO/Bloom post-processing in URP | +5–10 FPS |
| 🟡 7 | Reduce Render Scale to 0.85 in URP renderer | +5 FPS |
| 🟢 8 | Object pooling for tiles/coins | Smoother level loads |

---

## ✅ Expected Results After Fixes #1–4

| Metric | Before | After |
|--------|--------|-------|
| Build size | ~1 GB | **~80–150 MB** |
| FPS | ~15 | **40–60 FPS** |
| Load time | Slow | Fast |

> [!IMPORTANT]
> Start with **Fix #4** (Player Settings) first — it requires zero art changes and can be done in 10 minutes. It alone often cuts build size by 30–40%.

> [!TIP]
> After making changes, always do **Edit → Clear All PlayerPrefs** and then a **fresh build** (not incremental) to see accurate results. Use **File → Build Settings → Build Report** to check exactly what's eating space.

