# Optical Transfer

## Cross-Platform Software Color & Design Guide

### Windows · macOS · Linux · Android · iOS

---

## 1. Brand Direction

**Optical Transfer** should feel:
* Precise
* Technical
* Modern
* Intelligent
* Reliable
* Lightweight
* Slightly futuristic
* Professional rather than playful

The central visual idea is the relationship between the **O** and **T**:
* **O** = optical field, lens, transmission space, continuity
* **T** = transfer, direction, movement, signal
* **Tilt** = motion and technological dynamism
* **Green T** = active energy / transfer state
* **White O** = clarity / optical space
* **Black background** = technical environment / contrast

The UI should therefore use **high contrast, restrained green accents, geometric typography, and generous negative space**.

---

## 2. Core Brand Palette

| Name | HEX | RGB | Primary Use |
|---|---|---|---|
| Optical Green | **#98B878** | 152, 184, 120 | Main brand accent |
| Transfer Green | **#88A868** | 136, 168, 104 | Hover/active variations |
| Deep Optical | **#6F914F** | 111, 145, 79 | Strong UI accent |
| Dark Transfer | **#5C7F3F** | 92, 127, 63 | Accessible green text / buttons |
| Forest Optical | **#3F6126** | 63, 97, 38 | High-contrast green |
| Optical Black | **#0A0A0A** | 10, 10, 10 | Primary dark background |
| Pure Black | **#000000** | 0, 0, 0 | Logo / maximum contrast |
| Optical White | **#FFFFFF** | 255, 255, 255 | Primary text / logo |
| Soft White | **#F5F7F2** | 245, 247, 242 | Light-mode background |

### Recommended primary combination
**#0A0A0A + #98B878 + #FFFFFF**

---

## 3. Extended Neutral Palette (Dark Theme)

| Token | HEX | Usage |
|---|---|---|
| Black 100 | **#000000** | Maximum black |
| Black 95 | **#0A0A0A** | Main application background |
| Black 90 | **#111311** | Secondary background |
| Black 85 | **#181B18** | Cards / panels |
| Black 80 | **#202420** | Elevated surfaces |
| Gray 70 | **#2B302B** | Borders / separators |
| Gray 60 | **#3A403A** | Disabled borders |
| Gray 50 | **#555C55** | Secondary text |
| Gray 40 | **#737A73** | Muted text |
| Gray 20 | **#B7BDB7** | Secondary readable text |
| White 95 | **#F5F7F2** | Main text |
| White 100 | **#FFFFFF** | Maximum emphasis |

---

## 4. Light Theme

| Token | HEX | Usage |
|---|---|---|
| Background | **#F7F9F5** | Application background |
| Surface | **#FFFFFF** | Cards |
| Surface Alt | **#EEF2EB** | Secondary surfaces |
| Border | **#D9DED6** | Dividers |
| Border Strong | **#C3CBC0** | Active boundaries |
| Text Primary | **#101310** | Main text |
| Text Secondary | **#4D554C** | Secondary text |
| Text Muted | **#727A71** | Metadata |
| Brand | **#5C7F3F** | Primary accent |
| Brand Strong | **#3F6126** | Strong emphasis |

---

## 5. Semantic Color System

### Success
- Default: **#5E9F62**
- Light: **#A9D3A5**
- Dark: **#356C3A**
*(Used for: Transfer completed, Connection established, File verified)*

### Warning
- Default: **#D4A84F**
- Light: **#F0D99A**
- Dark: **#8C681E**
*(Used for: Slow transfer, Low storage, Unstable connection)*

### Error
- Default: **#C85A57**
- Light: **#E8AAA7**
- Dark: **#873735**
*(Used for: Transfer failed, Connection lost, Permission denied)*

### Information
- Default: **#668FA8**
- Light: **#ABC7D8**
- Dark: **#3D6178**

---

## 6. Accessibility & Contrast Rules

- **Use #98B878 for:** Large decorative areas, progress indicators, icons, graphics, active indicators.
- **Use #5C7F3F or darker for:** Green buttons containing white text, small green text, high-contrast controls.
- **Recommended Button:** `#5C7F3F` background + `#FFFFFF` text.

---

## 7. Color Ratio & Spacing

### Dark Interface Hierarchy:
- **70%:** Black / near-black (`#0A0A0A`, `#181B18`)
- **20%:** White / gray (`#FFFFFF`, `#F5F7F2`, `#B7BDB7`)
- **8%:** Neutral surfaces / borders (`#202420`, `#2B302B`)
- **2%:** Optical Green (`#98B878`)

### Base 4px Spacing Grid:
- `4px`, `8px`, `12px`, `16px`, `24px`, `32px`, `48px`, `64px`

---

## 8. Typography

- **Primary Typeface:** **Inter** (Regular 400, Medium 500, SemiBold 600, Bold 700)
- **Monospace / Technical:** **IBM Plex Sans** / SF Mono / Consolas
- **Display Typeface:** **Space Grotesk** (for hero titles and wordmarks)
- **Wordmark:** `OPTICAL TRANSFER` with generous tracking / letter-spacing.

---

## 9. Signature UI Motifs & Components

- **The Optical Transfer Ring:** Circular / elliptical ring surrounding a transfer indicator.
- **Buttons:**
  - Primary: `#5C7F3F` background, `#FFFFFF` text (Hover: `#6F914F`, Pressed: `#3F6126`)
  - Secondary: `#202420` background, `#3A403A` border, `#F5F7F2` text
  - Ghost: transparent background, `#B7BDB7` text (Hover: `#181B18`, `#F5F7F2`)
- **Cards:** `#181B18` background, `#2B302B` 1px border, 12px corner radius.
- **Inputs:** `#111311` background, `#3A403A` border (Focus: `#98B878` border + `rgba(152,184,120,0.25)` ring).
- **Progress Bar:** Track `#2B302B`, Progress `#98B878`, Completed `#5E9F62`, Failed `#C85A57`.
- **Icons:** Lucide-style line icons (1.5–2px stroke, rounded joins, geometric).
