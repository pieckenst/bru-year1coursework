# COSMIC Desktop Component Library for Slint

This directory contains the COSMIC Desktop-styled component library for the Slint UI framework. COSMIC Desktop is the modern desktop environment developed by System76 for Pop!_OS, featuring a clean, minimalist aesthetic with excellent light/dark mode support.

## Directory Structure

```
cosmic-1.0/
├── cosmic.slint           # Main entry point - exports all components and styling
├── font/                  # Inter font family (54 font files)
│   └── Inter_*.ttf       # Various weights and styles of Inter font
└── ui/
    ├── styling/          # Theme system definitions
    │   ├── palette.slint         # Color system (light/dark modes)
    │   ├── typography.slint      # Font scales and weights
    │   ├── spacing.slint         # Spacing scale (4px-based)
    │   ├── corner-radii.slint    # Border radius definitions
    │   └── elevation.slint       # Shadow system
    ├── components/       # UI components
    │   ├── button.slint          # Button variants
    │   ├── input.slint           # Text inputs
    │   ├── card.slint            # Cards and containers
    │   ├── dialog.slint          # Dialogs and modals
    │   ├── drawer.slint          # Navigation drawer
    │   ├── app-bar.slint         # Application bar
    │   └── ...                   # 70+ components total
    └── icons/            # Icon assets (to be populated)
```

## Design Principles

The COSMIC Desktop design system emphasizes:

- **Minimalism**: Clean, uncluttered interfaces with purposeful use of space
- **Consistency**: Unified design language across all components
- **Accessibility**: WCAG 2.1 AA compliance with high contrast and clear focus indicators
- **Adaptability**: Seamless light/dark mode support with appropriate color adjustments
- **Modern Aesthetics**: Rounded corners, subtle shadows, and smooth transitions

## Color System

- **Light Mode**: Light neutral backgrounds (#F5F5F5), white surfaces, dark text
- **Dark Mode**: Dark neutral backgrounds (#1A1A1A), elevated surfaces (#2B2B2B), light text
- **Accent Colors**: Configurable (default: brownish #8B7355 or orange #FF6B35)
- **Semantic Colors**: Error (red), Success (green), Warning (orange), Info (blue)

## Typography

- **Font Family**: Inter (included in font/ directory)
- **Scale**: 12px, 14px, 16px, 18px, 20px, 24px, 29px, 32px, 35px
- **Weights**: 300 (Light), 400 (Regular), 500 (Medium), 600 (SemiBold), 700 (Bold)
- **Presets**: title1-4, heading, body, caption, caption-heading

## Spacing

Based on 4px increments:
- xxxs: 4px, xxs: 8px, xs: 12px, s: 16px, m: 24px, l: 32px, xl: 48px, xxl: 64px

## Usage

Import the library in your Slint files:

```slint
import { CosmicButton, CosmicCard, CosmicDialog } from "../cosmic-1.0/cosmic.slint";
```

## Implementation Status

This library is currently under development. Components will be implemented progressively according to the task list in `.kiro/specs/cosmic-desktop-styling/tasks.md`.

## License

MIT License - Following COSMIC Desktop's open-source principles
