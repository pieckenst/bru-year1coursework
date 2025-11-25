# Design Document

## Overview

This design document outlines the architecture and implementation approach for transitioning the Rust/Slint-based Linux application from Material Design to COSMIC Desktop styling. COSMIC Desktop, developed by System76 for Pop!_OS, represents a modern, minimalist design language that emphasizes clarity, consistency, and user comfort across light and dark modes.

### Visual Design Language

Based on COSMIC Desktop's design system, the visual language emphasizes:

**Panel and Settings Layout:**
- Dark themed panels with subtle surface elevation (dark gray #2B2B2B on darker background #1A1A1A)
- Clean two-column settings layout with labels on left, controls on right
- Toggle switches with rounded pill shape, smooth animations, and accent color when active
- Sliders with thin tracks (4px), circular thumbs (16px), and accent-colored progress fill
- Generous spacing between settings rows (16-20px) for breathing room
- Section headers with medium font weight to organize related settings

**Theme Selection and Cards:**
- Grid layout of theme preview cards showing miniature desktop representations
- Cards with rounded corners (12-16px), subtle borders, and hover states
- Theme previews display actual color schemes (COSMIC Dark, COSMIC Light, Comet Light, Mocha Dark, Nebula Dark)
- Selected card indicated by accent-colored border or checkmark
- Card dimensions maintain consistent aspect ratio for desktop preview
- Smooth transitions on hover and selection states

**Navigation and Sidebar:**
- Left sidebar navigation with icon + text layout
- Navigation items at 44px height with 8px border radius
- Active items highlighted with accent color background at 15% opacity
- Hover states show subtle background at 50% surface color opacity
- Icons at 20-24px size, vertically centered with text
- Sidebar background slightly different from main content (2-5% variation)
- Section dividers or spacing to group related navigation items

**Window Controls and Header:**
- Minimalist header bar at 32-40px height
- Window control buttons (minimize, maximize, close) with symbolic icons at 16px
- Controls show at full opacity when focused, 75% when unfocused
- Draggable header area for window movement
- Clean title text at 18-20px with semibold weight

**Color and Contrast:**
- High contrast text on backgrounds (near-black #1A1A1A on light, near-white #F5F5F5 on dark)
- Accent colors used sparingly for interactive elements and highlights
- Neutral backgrounds with subtle variations for layering (#F5F5F5, #E8E8E8 in light mode)
- Borders and dividers at low opacity for subtle separation without harsh lines

The redesign will focus on:
1. **Visual Refresh**: Updating colors, typography, spacing, and component styling to match COSMIC Desktop aesthetics
2. **Component Library Migration**: Creating a new cosmic-1.0 component library to replace material-1.0
3. **Theme System**: Implementing robust light/dark mode support with COSMIC-inspired color palettes
4. **Consistency**: Ensuring all UI components follow the same design principles
5. **Accessibility**: Maintaining or improving accessibility standards with WCAG 2.1 AA compliance
6. **Comprehensive Component Set**: Implementing 70+ components covering all UI patterns

This is a styling-focused redesign that preserves all existing functionality while modernizing the visual appearance.

## Architecture

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         Application Layer (Unchanged)                        │
│                    Rust Logic, API Clients, State Management                 │
└──────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                          Slint UI Layer (Updated)                            │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                      Application Views                                 │  │
│  │  (bus_management.slint, route_management.slint, etc.)                 │  │
│  │                    Import cosmic-1.0 components                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                      cosmic-1.0 Component Library (New)                      │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                         cosmic.slint                                   │  │
│  │              Main entry point, theme configuration                     │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                      ui/styling/                                       │  │
│  │  - palette.slint (color definitions)                                  │  │
│  │  - typography.slint (font scales)                                     │  │
│  │  - spacing.slint (spacing scale)                                      │  │
│  │  - elevation.slint (shadow definitions)                               │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                      ui/components/                                    │  │
│  │  - button.slint, input.slint, card.slint                              │  │
│  │  - app_bar.slint, drawer.slint, dialog.slint                          │  │
│  │  - list.slint, table.slint, etc.                                      │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                         font/                                          │  │
│  │              Inter font family (reused from material-1.0)              │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Migration Strategy

The migration will follow a phased approach:

1. **Phase 1: Foundation** - Create cosmic-1.0 directory structure and define theme system
2. **Phase 2: Core Components** - Implement essential components (buttons, inputs, cards)
3. **Phase 3: Layout Components** - Implement app bar, drawer, dialogs
4. **Phase 4: Application Integration** - Update application views to use cosmic-1.0
5. **Phase 5: Testing & Refinement** - Test across all views and refine styling


## Components and Interfaces

### 1. Theme System (ui/styling/)

#### palette.slint

Defines the color system for light and dark modes:

```slint
export global CosmicPalette {
    // Mode selection
    in-out property <bool> dark-mode: false;
    
    // Light mode colors
    property <color> light-background: #F5F5F5;
    property <color> light-surface: #FFFFFF;
    property <color> light-surface-variant: #E8E8E8;
    property <color> light-text-primary: #1A1A1A;
    property <color> light-text-secondary: #666666;
    property <color> light-border: #D0D0D0;
    
    // Dark mode colors
    property <color> dark-background: #1A1A1A;
    property <color> dark-surface: #2B2B2B;
    property <color> dark-surface-variant: #242424;
    property <color> dark-text-primary: #F5F5F5;
    property <color> dark-text-secondary: #AAAAAA;
    property <color> dark-border: #404040;
    
    // Accent colors (configurable)
    in-out property <color> accent-primary: #8B7355;  // Brownish default
    in-out property <color> accent-hover: #9D8366;
    in-out property <color> accent-pressed: #79634A;
    
    // Semantic colors
    property <color> error: #E74C3C;
    property <color> success: #27AE60;
    property <color> warning: #F39C12;
    property <color> info: #3498DB;
    
    // Computed colors based on mode
    out property <color> background: dark-mode ? dark-background : light-background;
    out property <color> surface: dark-mode ? dark-surface : light-surface;
    out property <color> surface-variant: dark-mode ? dark-surface-variant : light-surface-variant;
    out property <color> text-primary: dark-mode ? dark-text-primary : light-text-primary;
    out property <color> text-secondary: dark-mode ? dark-text-secondary : light-text-secondary;
    out property <color> border: dark-mode ? dark-border : light-border;
}
```

#### typography.slint

Defines the typography scale:

```slint
export global CosmicTypography {
    // Font family (Inter)
    property <string> font-family: "Inter";
    
    // Font sizes
    property <length> size-xs: 12px;
    property <length> size-sm: 14px;
    property <length> size-base: 16px;
    property <length> size-lg: 18px;
    property <length> size-xl: 20px;
    property <length> size-2xl: 24px;
    property <length> size-3xl: 32px;
    
    // Font weights
    property <int> weight-regular: 400;
    property <int> weight-medium: 500;
    property <int> weight-semibold: 600;
    property <int> weight-bold: 700;
    
    // Line heights
    property <length> line-height-tight: 1.25;
    property <length> line-height-normal: 1.5;
    property <length> line-height-relaxed: 1.75;
}
```

#### spacing.slint

Defines the spacing scale:

```slint
export global CosmicSpacing {
    property <length> xs: 4px;
    property <length> sm: 8px;
    property <length> md: 12px;
    property <length> lg: 16px;
    property <length> xl: 20px;
    property <length> 2xl: 24px;
    property <length> 3xl: 32px;
    property <length> 4xl: 48px;
}
```

#### elevation.slint

Defines shadow styles:

```slint
export global CosmicElevation {
    // Shadow definitions
    property <length> shadow-sm-offset-y: 1px;
    property <length> shadow-sm-blur: 2px;
    property <color> shadow-sm-color: #00000019;  // 10% opacity
    
    property <length> shadow-md-offset-y: 2px;
    property <length> shadow-md-blur: 4px;
    property <color> shadow-md-color: #00000026;  // 15% opacity
    
    property <length> shadow-lg-offset-y: 4px;
    property <length> shadow-lg-blur: 8px;
    property <color> shadow-lg-color: #00000033;  // 20% opacity
}
```


### 2. Core Components (ui/components/)

#### Button Component (button.slint)

Based on COSMIC's button implementation with variants for icon, text, and image buttons:

```slint
// Base button builder pattern (inspired by COSMIC's Builder pattern)
export component CosmicButton {
    in property <string> text;
    in property <ButtonVariant> variant: ButtonVariant.standard;
    in property <ButtonClass> class: ButtonClass.standard;
    in property <bool> disabled: false;
    in property <bool> selected: false;
    in property <image> leading-icon;
    in property <image> trailing-icon;
    in property <string> tooltip;
    
    // Size presets (from COSMIC button sizing)
    in property <length> button-height: 32px;  // Can be 32px (default), 44px (large)
    in property <length> icon-size: 16px;      // 16px for symbolic, 24px for regular
    in property <length> font-size: 14px;
    in property <int> font-weight: 500;        // Medium weight
    
    callback clicked();
    
    // Computed styles based on variant and class
    property <color> background-color: {
        if variant == ButtonVariant.standard {
            class == ButtonClass.suggested ? CosmicPalette.accent-primary :
            class == ButtonClass.destructive ? CosmicPalette.error :
            class == ButtonClass.text ? Colors.transparent :
            CosmicPalette.surface
        } else {
            Colors.transparent
        }
    };
    
    property <color> text-color: {
        if class == ButtonClass.suggested || class == ButtonClass.destructive {
            #FFFFFF
        } else if class == ButtonClass.text || class == ButtonClass.link {
            CosmicPalette.accent-primary
        } else {
            CosmicPalette.text-primary
        }
    };
    
    property <length> border-width: {
        if class == ButtonClass.text || class == ButtonClass.link {
            0px
        } else if variant == ButtonVariant.standard {
            0px
        } else {
            1px
        }
    };
    
    states [
        disabled when root.disabled: {
            opacity: 0.4;
        }
        selected when root.selected: {
            background-color: CosmicPalette.accent-primary.with-alpha(0.15);
        }
        hover when touch-area.has-hover && !root.disabled: {
            background-color: {
                if class == ButtonClass.suggested {
                    CosmicPalette.accent-hover
                } else if class == ButtonClass.destructive {
                    CosmicPalette.error.darker(0.1)
                } else {
                    CosmicPalette.surface-variant
                }
            };
        }
        pressed when touch-area.pressed && !root.disabled: {
            background-color: {
                if class == ButtonClass.suggested {
                    CosmicPalette.accent-pressed
                } else {
                    CosmicPalette.surface-variant.darker(0.05)
                }
            };
        }
    ]
    
    Rectangle {
        background: root.background-color;
        border-radius: 10px;
        border-width: root.border-width;
        border-color: root.text-color;
        
        drop-shadow-offset-y: CosmicElevation.shadow-sm-offset-y;
        drop-shadow-blur: CosmicElevation.shadow-sm-blur;
        drop-shadow-color: CosmicElevation.shadow-sm-color;
        
        HorizontalLayout {
            padding-left: CosmicSpacing.lg;
            padding-right: CosmicSpacing.lg;
            padding-top: CosmicSpacing.md;
            padding-bottom: CosmicSpacing.md;
            spacing: CosmicSpacing.sm;
            alignment: center;
            
            if root.leading-icon.width > 0: Image {
                source: root.leading-icon;
                width: root.icon-size;
                height: root.icon-size;
                colorize: root.text-color;
            }
            
            if root.text != "": Text {
                text: root.text;
                color: root.text-color;
                font-family: CosmicTypography.font-family;
                font-size: root.font-size;
                font-weight: root.font-weight;
                vertical-alignment: center;
            }
            
            if root.trailing-icon.width > 0: Image {
                source: root.trailing-icon;
                width: root.icon-size;
                height: root.icon-size;
                colorize: root.text-color;
            }
        }
        
        touch-area := TouchArea {
            clicked => {
                if !root.disabled {
                    root.clicked();
                }
            }
        }
    }
}

// Icon-only button variant (for app bar actions, etc.)
export component CosmicIconButton {
    in property <image> icon;
    in property <bool> selected: false;
    in property <length> size: 40px;
    in property <length> icon-size: 20px;
    
    callback clicked();
    
    Rectangle {
        width: root.size;
        height: root.size;
        border-radius: 8px;
        background: touch-area.has-hover ? CosmicPalette.surface-variant : Colors.transparent;
        
        Image {
            source: root.icon;
            width: root.icon-size;
            height: root.icon-size;
            colorize: CosmicPalette.text-primary;
            x: (parent.width - self.width) / 2;
            y: (parent.height - self.height) / 2;
        }
        
        touch-area := TouchArea {
            clicked => { root.clicked(); }
        }
    }
}

export enum ButtonVariant {
    standard,
    icon,
    text,
}

export enum ButtonClass {
    standard,
    suggested,    // Accent color (primary action)
    destructive,  // Red/error color (delete, etc.)
    text,         // Text-only, no background
    link,         // Hyperlink style
    icon,         // Icon-only
    menu-root,    // Menu bar root items
    menu-item,    // Menu dropdown items
    header-bar,   // Header bar buttons
}
```

#### Input Component (input.slint)

```slint
export component CosmicInput {
    in-out property <string> text;
    in property <string> placeholder;
    in property <bool> disabled: false;
    in property <bool> has-error: false;
    in property <string> error-message;
    
    callback edited(string);
    callback accepted(string);
    
    property <bool> is-focused: false;
    
    VerticalLayout {
        spacing: CosmicSpacing.xs;
        
        Rectangle {
            background: CosmicPalette.surface;
            border-radius: 8px;
            border-width: is-focused ? 2px : 1px;
            border-color: has-error ? CosmicPalette.error : 
                         (is-focused ? CosmicPalette.accent-primary : CosmicPalette.border);
            
            HorizontalLayout {
                padding: CosmicSpacing.md;
                
                input := TextInput {
                    text <=> root.text;
                    placeholder-text: root.placeholder;
                    enabled: !root.disabled;
                    color: CosmicPalette.text-primary;
                    font-family: CosmicTypography.font-family;
                    font-size: CosmicTypography.size-base;
                    
                    edited => {
                        root.edited(self.text);
                    }
                    
                    accepted => {
                        root.accepted(self.text);
                    }
                }
            }
            
            states [
                focused when input.has-focus: {
                    is-focused: true;
                }
            ]
        }
        
        if root.has-error && root.error-message != "": Text {
            text: root.error-message;
            color: CosmicPalette.error;
            font-family: CosmicTypography.font-family;
            font-size: CosmicTypography.size-sm;
        }
    }
}
```


#### Card Component (card.slint)

```slint
export component CosmicCard {
    in property <string> title;
    in property <bool> interactive: false;
    
    callback clicked();
    
    property <bool> is-hovered: false;
    
    Rectangle {
        background: CosmicPalette.surface;
        border-radius: 14px;
        
        drop-shadow-offset-y: is-hovered ? CosmicElevation.shadow-md-offset-y : CosmicElevation.shadow-sm-offset-y;
        drop-shadow-blur: is-hovered ? CosmicElevation.shadow-md-blur : CosmicElevation.shadow-sm-blur;
        drop-shadow-color: is-hovered ? CosmicElevation.shadow-md-color : CosmicElevation.shadow-sm-color;
        
        VerticalLayout {
            padding: CosmicSpacing.xl;
            spacing: CosmicSpacing.lg;
            
            if root.title != "": Text {
                text: root.title;
                color: CosmicPalette.text-primary;
                font-family: CosmicTypography.font-family;
                font-size: CosmicTypography.size-lg;
                font-weight: CosmicTypography.weight-semibold;
            }
            
            @children
        }
        
        if root.interactive: TouchArea {
            mouse-cursor: pointer;
            
            clicked => {
                root.clicked();
            }
            
            states [
                hover when self.has-hover: {
                    is-hovered: true;
                }
            ]
        }
    }
}
```

#### List Item Component (list-item.slint)

```slint
export component CosmicListItem {
    in property <bool> selected: false;
    in property <bool> alternating: false;
    
    callback clicked();
    
    property <bool> is-hovered: false;
    
    Rectangle {
        background: selected ? CosmicPalette.accent-primary.with-alpha(0.15) :
                   (is-hovered ? CosmicPalette.surface-variant.with-alpha(0.5) :
                   (alternating ? CosmicPalette.surface-variant.with-alpha(0.03) : Colors.transparent));
        
        HorizontalLayout {
            padding-left: CosmicSpacing.lg;
            padding-right: CosmicSpacing.lg;
            padding-top: CosmicSpacing.md;
            padding-bottom: CosmicSpacing.md;
            
            @children
        }
        
        TouchArea {
            mouse-cursor: pointer;
            
            clicked => {
                root.clicked();
            }
            
            states [
                hover when self.has-hover: {
                    is-hovered: true;
                }
            ]
        }
    }
}
```

### 3. Layout Components

#### App Bar Component (app_bar.slint)

```slint
export component CosmicAppBar {
    in property <string> title;
    in property <[AppBarAction]> actions;
    
    callback action-clicked(int);
    
    Rectangle {
        background: CosmicPalette.surface;
        
        drop-shadow-offset-y: CosmicElevation.shadow-sm-offset-y;
        drop-shadow-blur: CosmicElevation.shadow-sm-blur;
        drop-shadow-color: CosmicElevation.shadow-sm-color;
        
        HorizontalLayout {
            padding-left: CosmicSpacing.2xl;
            padding-right: CosmicSpacing.2xl;
            padding-top: CosmicSpacing.lg;
            padding-bottom: CosmicSpacing.lg;
            spacing: CosmicSpacing.lg;
            
            Text {
                text: root.title;
                color: CosmicPalette.text-primary;
                font-family: CosmicTypography.font-family;
                font-size: CosmicTypography.size-xl;
                font-weight: CosmicTypography.weight-semibold;
                vertical-alignment: center;
                horizontal-stretch: 1;
            }
            
            HorizontalLayout {
                spacing: CosmicSpacing.sm;
                
                for action[index] in root.actions: Rectangle {
                    width: 40px;
                    height: 40px;
                    border-radius: 8px;
                    background: action-touch.has-hover ? CosmicPalette.surface-variant : Colors.transparent;
                    
                    Image {
                        source: action.icon;
                        width: 20px;
                        height: 20px;
                        colorize: CosmicPalette.text-primary;
                        x: (parent.width - self.width) / 2;
                        y: (parent.height - self.height) / 2;
                    }
                    
                    action-touch := TouchArea {
                        mouse-cursor: pointer;
                        clicked => {
                            root.action-clicked(index);
                        }
                    }
                }
            }
        }
    }
}

export struct AppBarAction {
    icon: image,
    tooltip: string,
}
```


#### Drawer Component (drawer.slint)

```slint
export component CosmicDrawer {
    in property <[DrawerItem]> items;
    in property <int> selected-index: -1;
    in property <string> header-text;
    
    callback item-clicked(int);
    
    Rectangle {
        background: CosmicPalette.surface-variant;
        
        VerticalLayout {
            padding: CosmicSpacing.lg;
            spacing: CosmicSpacing.md;
            
            if root.header-text != "": Rectangle {
                height: 60px;
                
                Text {
                    text: root.header-text;
                    color: CosmicPalette.text-primary;
                    font-family: CosmicTypography.font-family;
                    font-size: CosmicTypography.size-xl;
                    font-weight: CosmicTypography.weight-bold;
                    vertical-alignment: center;
                }
            }
            
            for item[index] in root.items: Rectangle {
                height: 44px;
                border-radius: 8px;
                background: index == root.selected-index ? 
                    CosmicPalette.accent-primary.with-alpha(0.15) :
                    (item-touch.has-hover ? CosmicPalette.surface.with-alpha(0.5) : Colors.transparent);
                
                HorizontalLayout {
                    padding-left: CosmicSpacing.md;
                    padding-right: CosmicSpacing.md;
                    spacing: CosmicSpacing.md;
                    
                    Image {
                        source: item.icon;
                        width: 20px;
                        height: 20px;
                        colorize: index == root.selected-index ? 
                            CosmicPalette.accent-primary : CosmicPalette.text-primary;
                    }
                    
                    Text {
                        text: item.label;
                        color: index == root.selected-index ? 
                            CosmicPalette.accent-primary : CosmicPalette.text-primary;
                        font-family: CosmicTypography.font-family;
                        font-size: CosmicTypography.size-base;
                        font-weight: index == root.selected-index ? 
                            CosmicTypography.weight-medium : CosmicTypography.weight-regular;
                        vertical-alignment: center;
                    }
                }
                
                item-touch := TouchArea {
                    mouse-cursor: pointer;
                    clicked => {
                        root.item-clicked(index);
                    }
                }
            }
        }
    }
}

export struct DrawerItem {
    icon: image,
    label: string,
}
```

#### Dialog Component (dialog.slint)

```slint
export component CosmicDialog {
    in property <string> title;
    in property <bool> show: false;
    
    callback close-requested();
    
    if root.show: Rectangle {
        width: 100%;
        height: 100%;
        background: #00000066;  // 40% opacity backdrop
        
        Rectangle {
            width: min(600px, parent.width * 0.9);
            height: min(parent.height * 0.8, self.preferred-height);
            x: (parent.width - self.width) / 2;
            y: (parent.height - self.height) / 2;
            
            background: CosmicPalette.surface;
            border-radius: 18px;
            
            drop-shadow-offset-y: CosmicElevation.shadow-lg-offset-y;
            drop-shadow-blur: CosmicElevation.shadow-lg-blur;
            drop-shadow-color: CosmicElevation.shadow-lg-color;
            
            VerticalLayout {
                padding: CosmicSpacing.2xl;
                spacing: CosmicSpacing.xl;
                
                // Header
                HorizontalLayout {
                    spacing: CosmicSpacing.lg;
                    
                    Text {
                        text: root.title;
                        color: CosmicPalette.text-primary;
                        font-family: CosmicTypography.font-family;
                        font-size: CosmicTypography.size-2xl;
                        font-weight: CosmicTypography.weight-semibold;
                        vertical-alignment: center;
                        horizontal-stretch: 1;
                    }
                    
                    Rectangle {
                        width: 32px;
                        height: 32px;
                        border-radius: 6px;
                        background: close-touch.has-hover ? CosmicPalette.surface-variant : Colors.transparent;
                        
                        Text {
                            text: "✕";
                            color: CosmicPalette.text-primary;
                            font-size: CosmicTypography.size-xl;
                            horizontal-alignment: center;
                            vertical-alignment: center;
                        }
                        
                        close-touch := TouchArea {
                            mouse-cursor: pointer;
                            clicked => {
                                root.close-requested();
                            }
                        }
                    }
                }
                
                // Content
                Rectangle {
                    vertical-stretch: 1;
                    @children
                }
            }
        }
        
        // Backdrop touch area
        TouchArea {
            clicked => {
                root.close-requested();
            }
        }
    }
}
```


## Component Catalog

This section provides a comprehensive overview of all components needed for the COSMIC Desktop styling system, organized by category.

### Foundation Components (4 components)

**1. CosmicPalette** - Global color system with light/dark mode support, accent color configuration, and semantic colors
**2. CosmicTypography** - Typography scale with Inter font family, size presets (12px-35px), and weight definitions
**3. CosmicSpacing** - Spacing scale based on 4px increments (4px-64px) for consistent layout
**4. CosmicElevation** - Shadow system with three elevation levels (sm, md, lg) for depth hierarchy

### Button Components (8 components)

**5. CosmicButton** - Primary button with variants (standard, suggested, destructive, text), states (normal, hover, pressed, focused, disabled, loading), and icon support
**6. CosmicIconButton** - Icon-only button for compact actions, supports symbolic (16px) and regular (24px) icons, with optional labels
**7. CosmicLinkButton** - Hyperlink-style button with underline on hover, accent color text, and optional trailing icon
**8. CosmicImageButton** - Image-based button with selection indicator, remove action, and hover overlay
**9. CosmicSegmentedButton** - Grouped button control for related options, supports single/multiple selection
**10. CosmicMenuButton** - Button that opens dropdown menu, with arrow indicator and keyboard shortcuts
**11. CosmicSplitButton** - Button with primary action and dropdown for additional options
**12. CosmicFloatingActionButton** - Circular elevated button for primary screen action

### Input Components (10 components)

**13. CosmicTextInput** - Single-line text input with focus states, error display, placeholder, clear button, and character counter
**14. CosmicTextArea** - Multi-line text input with minimum 3 rows, auto-resize, and character counter
**15. CosmicSearchInput** - Search field with search icon, clear button, loading spinner, and autocomplete suggestions
**16. CosmicPasswordInput** - Password field with show/hide toggle and strength indicator
**17. CosmicNumberInput** - Numeric input with increment/decrement buttons and validation
**18. CosmicCheckBox** - Checkbox with checked, unchecked, and indeterminate states, supports labels
**19. CosmicRadioButton** - Radio button for single selection from group, with label support
**20. CosmicToggle** - Toggle switch with pill shape, smooth animation, loading state, and optional description
**21. CosmicSlider** - Single-value slider with 4px track, 16px thumb, value labels, and tick marks
**22. CosmicRangeSlider** - Dual-thumb slider for range selection with min/max values

### Selection Components (5 components)

**23. CosmicDropdown** - Dropdown menu for list selection, supports search, multi-select, groups, and icons
**24. CosmicComboBox** - Editable dropdown with autocomplete and custom value entry
**25. CosmicSelect** - Native-style select component with keyboard navigation
**26. CosmicMultiSelect** - Multi-selection dropdown with chips for selected items
**27. CosmicAutocomplete** - Input with suggestion dropdown based on user typing

### Display Components (8 components)

**28. CosmicCard** - Container with title, content area, elevated surface, hover effects, and optional actions
**29. CosmicBadge** - Count indicator with 20px height, accent color, positioned on parent element
**30. CosmicChip** - Removable tag with avatar support, 28px height, and clickable variants
**31. CosmicAvatar** - User representation with circular/square shapes, sizes (24px-56px), initials fallback, and status indicator
**32. CosmicAvatarGroup** - Overlapping avatar display with overflow count
**33. CosmicTooltip** - Contextual information overlay with 500ms delay, arrow pointer, and max 300px width
**34. CosmicLabel** - Text label with semantic color variants (default, success, warning, error, info)
**35. CosmicDivider** - Horizontal/vertical separator with optional label, 1px thickness

### Feedback Components (7 components)

**36. CosmicSpinner** - Rotating loading indicator in three sizes (16px, 32px, 64px)
**37. CosmicProgressBar** - Linear progress with determinate/indeterminate modes, 4px height
**38. CosmicCircularProgress** - Circular progress indicator with optional percentage display
**39. CosmicSkeletonLoader** - Animated placeholder for loading content with pulsing effect
**40. CosmicAlert** - Notification banner with semantic colors, icon, close button, and action buttons
**41. CosmicToast** - Temporary notification with auto-dismiss, positioned at screen edges
**42. CosmicSnackbar** - Bottom notification with action button and swipe-to-dismiss

### Layout Components (9 components)

**43. CosmicAppBar** - Top application bar with title, action buttons, 40-48px height, and minimal elevation
**44. CosmicHeaderBar** - Window header with title, window controls (minimize, maximize, close), and draggable area
**45. CosmicDrawer** - Navigation sidebar with icon+text layout, 44px item height, active state highlighting
**46. CosmicNavigationBar** - Bottom navigation for mobile with icon+label items
**47. CosmicTabBar** - Horizontal tabs with 44px height, 76-250px width, close buttons, and drag-to-reorder
**48. CosmicBreadcrumb** - Navigation path with separators, collapsible middle items, and clickable links
**49. CosmicStepper** - Step indicator for multi-step processes with completed/active/upcoming states
**50. CosmicSpacer** - Flexible spacing component for layout control
**51. CosmicContainer** - Generic container with padding, borders, and background options

### Overlay Components (6 components)

**52. CosmicDialog** - Modal dialog with backdrop, title, content area, action buttons, and sizes (400px, 600px, 800px)
**53. CosmicPopover** - Contextual overlay positioned relative to trigger, with viewport containment
**54. CosmicMenu** - Dropdown menu with items, submenus, dividers, keyboard shortcuts, and checkboxes
**55. CosmicContextMenu** - Right-click menu with adaptive positioning
**56. CosmicBottomSheet** - Mobile-style bottom overlay with drag handle
**57. CosmicSidebar** - Slide-out panel from edges with modal/non-modal modes

### List Components (5 components)

**58. CosmicListItem** - List row with selection, hover, alternating backgrounds (zebra striping)
**59. CosmicList** - Scrollable list container with virtual scrolling for large datasets
**60. CosmicTreeView** - Hierarchical tree with expand/collapse, indentation, checkboxes, and drag-drop
**61. CosmicAccordion** - Expandable content sections with animated height transitions
**62. CosmicExpansionPanel** - Single expandable panel with header and content

### Table Components (4 components)

**63. CosmicDataTable** - Data table with sortable columns, resizable columns, row selection, zebra striping, and pagination
**64. CosmicTableHeader** - Table header with sort indicators and filter inputs
**65. CosmicTableRow** - Table row with hover, selection, and action states
**66. CosmicTableCell** - Table cell with text alignment and overflow handling

### Date/Time Components (4 components)

**67. CosmicDatePicker** - Calendar-based date selector with month/year navigation
**68. CosmicTimePicker** - Hour/minute selector with 12/24-hour formats
**69. CosmicDateTimePicker** - Combined date and time selection
**70. CosmicDateRangePicker** - Date range selector with start/end dates

### File Components (2 components)

**71. CosmicFileUpload** - Drag-drop file upload area with progress bars, file list, and validation
**72. CosmicFileInput** - File selection button with selected file display

### Pagination Components (2 components)

**73. CosmicPagination** - Page navigation with prev/next, page numbers, ellipsis, and jump-to-page
**74. CosmicItemsPerPage** - Dropdown for selecting items per page (10, 25, 50, 100)

### Utility Components (2 components)

**75. CosmicFocusIndicator** - 2px accent-colored outline for keyboard focus
**76. CosmicScrollbar** - Minimal 6-8px scrollbar with auto-hide behavior

### Total: 76 Components

Each component follows COSMIC Desktop design principles:
- Consistent use of spacing scale (4px increments)
- Border radius system (0px, 4px, 8px, 12px, 16px, 20px, 24px, 9999px)
- Typography scale with Inter font family
- Light/dark mode support with semantic color naming
- Accessibility compliance (WCAG 2.1 AA)
- Smooth animations (150-300ms transitions)
- Keyboard navigation support
- Touch-friendly sizing (minimum 44px touch targets)


## Data Models

No new data models are required for this styling redesign. All existing Rust data models remain unchanged. The styling changes are purely visual and do not affect the data layer.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Color contrast meets accessibility standards
*For any* text displayed on any background, the contrast ratio should be at least 4.5:1 for normal text and 3:1 for large text.
**Validates: Requirements 19.1**

### Property 2: Theme consistency across components
*For any* component rendered, it should use colors from the CosmicPalette global and not hardcoded colors (except for semantic colors like error, success).
**Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 13.1-13.5, 14.1-14.5**

### Property 3: Typography scale consistency
*For any* text element, it should use font sizes and weights defined in CosmicTypography global.
**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

### Property 4: Spacing scale consistency
*For any* layout with padding or margins, it should use values from the CosmicSpacing global (multiples of 4px).
**Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

### Property 5: Border radius consistency
*For any* component with rounded corners, the border radius should match the values specified in the requirements (buttons: 8-12px, cards: 12-16px, inputs: 6-8px, dialogs: 16-20px).
**Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**

### Property 6: Button hover state provides visual feedback
*For any* enabled button, hovering should change the background color or shadow to indicate interactivity.
**Validates: Requirements 7.5**

### Property 7: Input focus state shows accent color
*For any* input field, when focused, the border should change to the accent color with 2px width.
**Validates: Requirements 8.2**

### Property 8: Disabled elements reduce opacity
*For any* disabled interactive element (button, input), the opacity should be reduced to 40-50%.
**Validates: Requirements 7.4**

### Property 9: List items provide hover feedback
*For any* list item, hovering should display a subtle background highlight.
**Validates: Requirements 9.2**

### Property 10: Selected list items show accent color
*For any* selected list item, the background should use the accent color with reduced opacity (10-15%).
**Validates: Requirements 9.3**

### Property 11: Dark mode inverts color scheme
*For any* component, when dark mode is enabled, it should use dark-mode colors from the palette instead of light-mode colors.
**Validates: Requirements 14.1, 14.2, 14.3, 14.4, 14.5**

### Property 12: Shadows adapt to elevation level
*For any* elevated component, the shadow should match the appropriate elevation level (sm, md, or lg) defined in CosmicElevation.
**Validates: Requirements 5.1, 5.2, 5.3, 5.4**

### Property 13: Interactive cards increase elevation on hover
*For any* interactive card, hovering should increase the shadow from sm to md elevation.
**Validates: Requirements 5.3, 18.4**

### Property 14: Dialog backdrop blocks interaction
*For any* open dialog, clicking the backdrop should trigger the close-requested callback.
**Validates: Requirements 10.1**

### Property 15: Navigation items show active state
*For any* selected navigation item, it should display the accent color background with reduced opacity and accent color text.
**Validates: Requirements 11.4**

### Property 16: Error states use error color
*For any* input field with an error, the border should be red and an error message should be displayed below.
**Validates: Requirements 8.5**

### Property 17: Accent color is configurable
*For any* accent color change, all components using the accent should update to reflect the new color.
**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

### Property 18: Font family is consistent
*For any* text element, it should use the Inter font family defined in CosmicTypography.
**Validates: Requirements 3.1**

### Property 19: Icon sizes are consistent
*For any* icon displayed, it should use the appropriate size for its context (16-20px for buttons, 20-24px for navigation).
**Validates: Requirements 12.3, 12.4**

### Property 20: Responsive layout adapts to window size
*For any* window resize, the layout should adjust spacing and component sizes appropriately without breaking.
**Validates: Requirements 20.1, 20.2, 20.3, 20.4**


## Error Handling

Since this is a styling redesign, error handling remains unchanged from the existing implementation. However, visual error states should be updated to match COSMIC Desktop styling:

### Error Display Patterns

1. **Input Validation Errors**
   - Red border (CosmicPalette.error) on invalid inputs
   - Error message displayed below input in red text
   - Error icon (optional) displayed next to error message

2. **API Errors**
   - Error messages displayed in dialogs or toast notifications
   - Use error color for background or border
   - Clear, actionable error text

3. **Loading States**
   - Subtle loading indicators using accent color
   - Disabled state on buttons during loading
   - Skeleton screens for content loading (optional enhancement)

## Testing Strategy

### Visual Regression Testing

1. **Component Screenshots**
   - Capture screenshots of each component in light and dark modes
   - Test with different accent colors (brownish and orange)
   - Verify hover, focus, and active states

2. **Accessibility Testing**
   - Use automated tools to verify contrast ratios
   - Test keyboard navigation
   - Verify focus indicators are visible

3. **Cross-View Testing**
   - Test all application views with new styling
   - Verify consistency across bus management, route management, etc.
   - Test dialogs and modals in each view

### Manual Testing Checklist

- [ ] Light mode renders correctly across all views
- [ ] Dark mode renders correctly across all views
- [ ] Accent color can be changed and updates all components
- [ ] All buttons have appropriate hover/press states
- [ ] All inputs show focus states correctly
- [ ] Lists show hover and selection states
- [ ] Dialogs display with correct backdrop and elevation
- [ ] Navigation shows active state correctly
- [ ] Typography is consistent and readable
- [ ] Spacing is consistent throughout
- [ ] Border radius is consistent on all components
- [ ] Shadows are subtle and appropriate
- [ ] Error states display correctly
- [ ] Disabled states are visually distinct
- [ ] Icons are properly sized and colored
- [ ] Responsive behavior works at different window sizes

### Property-Based Testing

Property-based tests will be written to verify the correctness properties defined above. These tests will:

1. Generate random component configurations
2. Verify that styling properties hold across all configurations
3. Test theme switching (light/dark mode)
4. Test accent color changes
5. Verify accessibility properties (contrast ratios)

The testing framework will use Slint's testing capabilities combined with Rust property-based testing libraries.


## Implementation Notes

### Directory Structure

The new cosmic-1.0 component library will follow this structure:

```
cosmic-1.0/
├── cosmic.slint                 # Main entry point
├── font/                        # Reuse Inter fonts from material-1.0
│   └── (Inter font files)
├── ui/
│   ├── styling/
│   │   ├── palette.slint       # Color definitions
│   │   ├── typography.slint    # Font scales
│   │   ├── spacing.slint       # Spacing scale
│   │   └── elevation.slint     # Shadow definitions
│   ├── components/
│   │   ├── button.slint        # Button component
│   │   ├── input.slint         # Input component
│   │   ├── card.slint          # Card component
│   │   ├── list-item.slint     # List item component
│   │   ├── app_bar.slint       # App bar component
│   │   ├── drawer.slint        # Drawer component
│   │   ├── dialog.slint        # Dialog component
│   │   ├── checkbox.slint      # Checkbox component
│   │   ├── radio.slint         # Radio button component
│   │   ├── select.slint        # Select/dropdown component
│   │   └── ...                 # Other components as needed
│   └── icons/
│       └── (icon files)
└── README.md
```

### Migration Steps

1. **Create cosmic-1.0 directory structure**
   - Copy font directory from material-1.0
   - Create ui/styling, ui/components, ui/icons directories

2. **Implement theme system**
   - Create palette.slint with color definitions
   - Create typography.slint with font scales
   - Create spacing.slint with spacing scale
   - Create elevation.slint with shadow definitions

3. **Implement core components**
   - Button (primary, secondary, danger variants)
   - Input (with error states)
   - Card (with interactive variant)
   - List item (with selection and hover states)

4. **Implement layout components**
   - App bar
   - Drawer
   - Dialog

5. **Update application views**
   - Change imports from material-1.0 to cosmic-1.0
   - Update component usage to match new API
   - Test each view individually

6. **Test and refine**
   - Visual testing in light and dark modes
   - Accessibility testing
   - Cross-view consistency check
   - Performance testing

### Accent Color Options

Two accent color options will be provided:

1. **Brownish (Default)**
   - Primary: #8B7355
   - Hover: #9D8366
   - Pressed: #79634A
   - Inspired by COSMIC Desktop calculator

2. **Orange (Alternative)**
   - Primary: #FF6B35
   - Hover: #FF7F4D
   - Pressed: #E65A2E
   - Vibrant alternative for users who prefer warmer tones

Users can switch between these or define custom accent colors by modifying the CosmicPalette.accent-primary property.

### Performance Considerations

1. **Minimize redraws**: Use computed properties efficiently
2. **Optimize shadows**: Use simple shadow definitions to avoid performance impact
3. **Lazy loading**: Load components only when needed
4. **Theme caching**: Cache theme calculations to avoid repeated computations

### Accessibility Considerations

1. **Contrast ratios**: All text must meet WCAG AA standards (4.5:1 for normal text, 3:1 for large text)
2. **Focus indicators**: All interactive elements must have visible focus indicators
3. **Keyboard navigation**: All functionality must be accessible via keyboard
4. **Screen reader support**: Use appropriate ARIA labels and semantic HTML where applicable
5. **Color independence**: Don't rely solely on color to convey information

### Future Enhancements

1. **Animation system**: Add subtle animations for state transitions
2. **Additional components**: Implement more specialized components as needed
3. **Theme variants**: Add more color scheme options
4. **Customization API**: Allow users to customize colors, spacing, etc.
5. **Component documentation**: Create comprehensive documentation with examples


### 4. Additional Components from COSMIC

#### Menu System (menu.slint)

COSMIC includes a sophisticated menu system with:
- Menu bar with root items
- Dropdown menus with nested submenus
- Keyboard shortcuts display
- Dividers between menu sections
- Checkbox menu items
- Folder menu items (submenus)

```slint
export component CosmicMenuBar {
    in property <[MenuItem]> items;
    in property <bool> open: false;
    
    callback item-clicked(int);
    
    // Menu bar implementation with dropdown support
    // Based on COSMIC's MenuBar widget
}

export struct MenuItem {
    label: string,
    icon: image,
    shortcut: string,
    children: [MenuItem],  // For submenus
    is-checkbox: bool,
    checked: bool,
    is-divider: bool,
}
```

#### Popover Component (popover.slint)

For context menus and tooltips:

```slint
export component CosmicPopover {
    in property <bool> modal: false;
    in property <PopoverPosition> position: PopoverPosition.center;
    
    callback close-requested();
    
    // Popover implementation with positioning logic
    // Based on COSMIC's Popover widget
}

export enum PopoverPosition {
    center,
    bottom,
    point,
}
```

#### Header Bar Component (header_bar.slint)

For window title bars with controls:

```slint
export component CosmicHeaderBar {
    in property <string> title;
    in property <bool> focused: false;
    in property <bool> maximized: false;
    in property <bool> show-window-controls: true;
    
    callback minimize-clicked();
    callback maximize-clicked();
    callback close-clicked();
    callback drag-requested();
    
    // Header bar with window controls
    // Based on COSMIC's HeaderBar widget
    // Height: 32px + padding (40-48px total)
    // Includes minimize, maximize, close buttons
}
```

#### Segmented Button Component (segmented_button.slint)

For tab bars and control groups:

```slint
export component CosmicSegmentedButton {
    in property <[SegmentItem]> items;
    in property <int> selected-index: -1;
    in property <bool> show-dividers: true;
    
    callback item-clicked(int);
    
    // Segmented button for tabs or control groups
    // Based on COSMIC's SegmentedButton widget
    // Button height: 32px or 44px
    // Dividers between items
}

export struct SegmentItem {
    text: string,
    icon: image,
    closeable: bool,
}
```

#### Warning/Alert Component (warning.slint)

For displaying warnings and alerts:

```slint
export component CosmicWarning {
    in property <string> message;
    
    callback close-clicked();
    
    Rectangle {
        background: CosmicPalette.warning;
        border-radius: CosmicSpacing.sm;
        
        HorizontalLayout {
            padding: CosmicSpacing.md;
            spacing: CosmicSpacing.md;
            
            Text {
                text: root.message;
                color: CosmicPalette.warning.on;  // Warning text color
                horizontal-stretch: 1;
            }
            
            CosmicIconButton {
                icon: @image-url("window-close-symbolic.svg");
                icon-size: 16px;
                size: 24px;
                clicked => { root.close-clicked(); }
            }
        }
    }
}
```

### 5. COSMIC Design Patterns

#### Size Presets

COSMIC defines size presets for buttons:
- **Extra Small**: 16px icon, 14px text, xxs padding
- **Small (Default)**: 16-20px icon, 14px text, xs padding
- **Medium**: 32px icon, 24px text, xs padding
- **Large**: 40px icon, 28px text, xs padding
- **Extra Large**: 56px icon, 32px text, xs padding

#### Spacing System

COSMIC uses a consistent spacing system:
- `space_xxxs`: 2px
- `space_xxs`: 4px
- `space_xs`: 8px
- `space_s`: 12px
- `space_m`: 16px
- `space_l`: 20px
- `space_xl`: 24px
- `space_xxl`: 32px

#### Corner Radii

COSMIC defines corner radius presets:
- `radius_0`: 0px (sharp corners)
- `radius_xs`: 4px
- `radius_s`: 8px
- `radius_m`: 12px
- `radius_l`: 16px
- `radius_xl`: 20px

#### Button Padding Patterns

From COSMIC button implementations:
- Icon buttons: 8px padding
- Text buttons: [4px, 12px] (vertical, horizontal)
- Standard buttons: [0px, space_s] with space_l height
- Menu items: [4px, 16px]
- Nav bar items: [space_s, space_xxs] with 32px height

#### State Management

COSMIC buttons track multiple states:
- `is_hovered`: Mouse is over the button
- `is_pressed`: Button is being clicked
- `is_focused`: Button has keyboard focus
- `selected`: Button represents selected state (for toggles)

#### Focus Indicators

COSMIC uses outline-based focus indicators:
- Outline width: 2px
- Outline color: Accent color or high-contrast color
- Outline offset: 2px from border

### 6. Slint-Specific Adaptations

When adapting COSMIC components to Slint:

1. **State Management**: Use Slint's `states` blocks instead of Rust state structs
2. **Callbacks**: Use Slint callbacks instead of message passing
3. **Layouts**: Use Slint's layout containers (HorizontalLayout, VerticalLayout)
4. **Animations**: Use Slint's animation system for smooth transitions
5. **Touch Areas**: Use TouchArea for mouse/touch interaction
6. **Conditional Rendering**: Use `if` statements for optional elements

#### Example State Adaptation

COSMIC (Iced):
```rust
states [
    disabled when !is_enabled: { opacity: 0.4; }
    hover when is_hovered: { background: hover_color; }
]
```

Slint equivalent:
```slint
states [
    disabled when root.disabled: {
        opacity: 0.4;
    }
    hover when touch-area.has-hover && !root.disabled: {
        background: hover-color;
    }
]
```

#### Icon Handling

COSMIC uses icon names (e.g., "window-close-symbolic"), while Slint uses image paths:
- Create icon mapping system
- Store icons in cosmic-1.0/ui/icons/
- Use @image-url() for icon references

#### Typography Adaptation

COSMIC typography presets to Slint:
- `title1`: 35px, semibold, 52px line height
- `title2`: 29px, semibold, 43px line height
- `title3`: 24px, bold, 36px line height
- `title4`: 20px, bold, 30px line height
- `heading`: 14px, bold, 21px line height
- `body`: 14px, regular, 21px line height
- `caption`: 12px, regular, 17px line height
- `caption-heading`: 12px, semibold, 17px line height
- `monotext`: 14px, mono font, 20px line height



### 14. Navigation Bar Component Design

Based on COSMIC's nav_bar implementation, the navigation sidebar provides:

```slint
export component CosmicNavBar {
    in property <[NavItem]> items;
    in property <int> active-index: -1;
    
    callback item-activated(int);
    callback item-closed(int);
    callback item-context-menu(int);
    callback item-middle-pressed(int);
    
    // Navigation bar with vertical segmented buttons
    VerticalLayout {
        spacing: CosmicSpacing.xxs;
        padding: CosmicSpacing.xxs;
        
        for item[index] in root.items: CosmicNavButton {
            text: item.text;
            icon: item.icon;
            selected: index == root.active-index;
            closable: item.closable;
            
            clicked => { root.item-activated(index); }
            close-clicked => { root.item-closed(index); }
            right-clicked => { root.item-context-menu(index); }
            middle-clicked => { root.item-middle-pressed(index); }
        }
    }
}

export struct NavItem {
    text: string,
    icon: image,
    closable: bool,
}

export component CosmicNavButton {
    in property <string> text;
    in property <image> icon;
    in property <bool> selected;
    in property <bool> closable;
    
    callback clicked();
    callback close-clicked();
    callback right-clicked();
    callback middle-clicked();
    
    min-height: 32px;
    // Button implementation with icon, text, and optional close button
}
```

### 15. Menu Bar and Dropdown System

COSMIC's menu system supports hierarchical menus with keyboard shortcuts:

```slint
export component CosmicMenuBar {
    in property <[MenuRoot]> menu-roots;
    
    callback menu-action(string /* action-id */);
    
    HorizontalLayout {
        spacing: 0px;
        
        for root in root.menu-roots: CosmicMenuRoot {
            text: root.label;
            items: root.items;
            
            action-triggered(id) => { root.menu-action(id); }
        }
    }
}

export struct MenuRoot {
    label: string,
    items: [MenuItem],
}

export struct MenuItem {
    label: string,
    icon: image,
    shortcut: string,
    action-id: string,
    enabled: bool,
    checked: bool,
    children: [MenuItem], // For submenus
}

export component CosmicMenuRoot {
    in property <string> text;
    in property <[MenuItem]> items;
    
    callback action-triggered(string);
    
    // Root menu button with dropdown
    CosmicButton {
        text: root.text;
        class: ButtonClass.MenuRoot;
        padding: [4px, 12px];
        
        clicked => {
            // Show dropdown menu
        }
    }
}

export component CosmicMenuItem {
    in property <MenuItem> item;
    
    callback activated();
    
    HorizontalLayout {
        spacing: CosmicSpacing.xxs;
        padding: [4px, 16px];
        height: 36px;
        
        // Checkbox indicator (if checked)
        if item.checked: CosmicIcon {
            source: @image-url("object-select-symbolic.svg");
            width: 16px;
            height: 16px;
            colorize: CosmicPalette.accent-primary;
        }
        
        // Icon (if present)
        if item.icon != @image-url(""): CosmicIcon {
            source: item.icon;
            width: 14px;
            height: 14px;
        }
        
        // Label
        Text {
            text: item.label;
            font-size: CosmicTypography.size-base;
            color: item.enabled ? CosmicPalette.text-primary : CosmicPalette.text-disabled;
            horizontal-stretch: 1;
        }
        
        // Shortcut
        Text {
            text: item.shortcut;
            font-size: CosmicTypography.size-sm;
            color: CosmicPalette.text-secondary;
        }
        
        // Submenu indicator
        if item.children.length > 0: CosmicIcon {
            source: @image-url("pan-end-symbolic.svg");
            width: 16px;
            height: 16px;
        }
    }
}
```

### 16. Segmented Button Component

COSMIC segmented buttons for grouped selections:

```slint
export component CosmicSegmentedButton {
    in property <[SegmentItem]> items;
    in property <int> active-index: -1;
    in property <SegmentOrientation> orientation: SegmentOrientation.horizontal;
    
    callback segment-activated(int);
    
    if orientation == SegmentOrientation.horizontal: HorizontalLayout {
        spacing: CosmicSpacing.xxs;
        
        for item[index] in root.items: CosmicSegment {
            text: item.text;
            icon: item.icon;
            selected: index == root.active-index;
            position: index == 0 ? SegmentPosition.first : 
                     (index == root.items.length - 1 ? SegmentPosition.last : SegmentPosition.middle);
            
            clicked => { root.segment-activated(index); }
        }
    }
    
    if orientation == SegmentOrientation.vertical: VerticalLayout {
        spacing: CosmicSpacing.xxs;
        
        for item[index] in root.items: CosmicSegment {
            text: item.text;
            icon: item.icon;
            selected: index == root.active-index;
            position: index == 0 ? SegmentPosition.first : 
                     (index == root.items.length - 1 ? SegmentPosition.last : SegmentPosition.middle);
            
            clicked => { root.segment-activated(index); }
        }
    }
}

export struct SegmentItem {
    text: string,
    icon: image,
}

export enum SegmentOrientation {
    horizontal,
    vertical,
}

export enum SegmentPosition {
    first,
    middle,
    last,
    only,
}

export component CosmicSegment {
    in property <string> text;
    in property <image> icon;
    in property <bool> selected;
    in property <SegmentPosition> position;
    
    callback clicked();
    
    height: 32px;
    min-width: 76px;
    max-width: 250px;
    
    // Segment button with appropriate border radius based on position
}
```

### 17. Tab Bar Component

COSMIC tab bar for content switching:

```slint
export component CosmicTabBar {
    in property <[TabItem]> tabs;
    in property <int> active-tab: 0;
    
    callback tab-activated(int);
    callback tab-closed(int);
    callback tab-context-menu(int);
    
    HorizontalLayout {
        spacing: CosmicSpacing.xxs;
        
        for tab[index] in root.tabs: CosmicTab {
            text: tab.text;
            icon: tab.icon;
            active: index == root.active-tab;
            closable: tab.closable;
            
            clicked => { root.tab-activated(index); }
            close-clicked => { root.tab-closed(index); }
            right-clicked => { root.tab-context-menu(index); }
        }
    }
}

export struct TabItem {
    text: string,
    icon: image,
    closable: bool,
}

export component CosmicTab {
    in property <string> text;
    in property <image> icon;
    in property <bool> active;
    in property <bool> closable;
    
    callback clicked();
    callback close-clicked();
    callback right-clicked();
    
    height: 44px;
    min-width: 76px;
    max-width: 250px;
    
    // Tab with active indicator and optional close button
}
```

### 18. Header Bar Component

COSMIC header bar with window controls:

```slint
export component CosmicHeaderBar {
    in property <string> title;
    in property <bool> focused: true;
    in property <bool> maximized: false;
    in property <bool> sharp-corners: false;
    
    callback close-clicked();
    callback minimize-clicked();
    callback maximize-clicked();
    callback drag-started();
    callback double-clicked();
    
    height: focused ? 40px : 36px;
    
    HorizontalLayout {
        spacing: 8px;
        padding: maximized ? [8px, 8px] : [7px, 7px, 8px, 7px];
        
        // Start region
        HorizontalLayout {
            spacing: CosmicSpacing.xxxs;
            horizontal-stretch: 1;
            
            @children-start
        }
        
        // Center region (title)
        Text {
            text: root.title;
            font-size: CosmicTypography.size-lg;
            font-weight: CosmicTypography.weight-semibold;
            color: CosmicPalette.text-primary;
            horizontal-stretch: 2;
            horizontal-alignment: center;
        }
        
        // End region with window controls
        HorizontalLayout {
            spacing: CosmicSpacing.xxs;
            horizontal-stretch: 1;
            horizontal-alignment: end;
            
            @children-end
            
            // Window controls
            CosmicIconButton {
                icon: @image-url("window-minimize-symbolic.svg");
                icon-size: 16px;
                class: ButtonClass.HeaderBar;
                selected: root.focused;
                
                clicked => { root.minimize-clicked(); }
            }
            
            CosmicIconButton {
                icon: root.maximized ? 
                      @image-url("window-restore-symbolic.svg") : 
                      @image-url("window-maximize-symbolic.svg");
                icon-size: 16px;
                class: ButtonClass.HeaderBar;
                selected: root.focused;
                
                clicked => { root.maximize-clicked(); }
            }
            
            CosmicIconButton {
                icon: @image-url("window-close-symbolic.svg");
                icon-size: 16px;
                class: ButtonClass.HeaderBar;
                selected: root.focused;
                
                clicked => { root.close-clicked(); }
            }
        }
    }
}
```

### 19. Popover Component

COSMIC popover for contextual overlays:

```slint
export component CosmicPopover {
    in property <bool> visible;
    in property <PopoverPosition> position: PopoverPosition.bottom;
    in property <bool> modal: false;
    
    callback closed();
    
    if visible: Rectangle {
        // Backdrop (if modal)
        if modal: Rectangle {
            background: CosmicPalette.overlay-backdrop;
            
            TouchArea {
                clicked => {
                    if !modal {
                        root.closed();
                    }
                }
            }
        }
        
        // Popover content
        Rectangle {
            background: CosmicPalette.surface-primary;
            border-radius: CosmicCornerRadii.radius-m;
            drop-shadow-blur: 16px;
            drop-shadow-color: CosmicPalette.shadow;
            drop-shadow-offset-y: 4px;
            
            @children
        }
    }
}

export enum PopoverPosition {
    top,
    bottom,
    left,
    right,
    center,
}
```

### 20. Warning Component

COSMIC warning/alert component:

```slint
export component CosmicWarning {
    in property <string> message;
    in property <bool> dismissible: true;
    
    callback dismissed();
    
    Rectangle {
        background: CosmicPalette.warning;
        border-radius: CosmicCornerRadii.radius-s;
        
        HorizontalLayout {
            padding: 10px;
            spacing: CosmicSpacing.xs;
            
            Text {
                text: root.message;
                color: CosmicPalette.warning-on;
                horizontal-stretch: 1;
                wrap: word-wrap;
            }
            
            if dismissible: CosmicIconButton {
                icon: @image-url("window-close-symbolic.svg");
                icon-size: 16px;
                
                clicked => { root.dismissed(); }
            }
        }
    }
}
```

### 21. Responsive Container Implementation

COSMIC responsive containers that adapt to size changes:

```slint
export component CosmicResponsiveContainer {
    in property <length> min-width: 320px;
    in property <length> max-width: 1200px;
    in property <bool> collapse-navigation: false;
    
    callback size-changed(length /* width */, length /* height */);
    
    // Container that monitors size and emits events
    Rectangle {
        min-width: root.min-width;
        max-width: root.max-width;
        
        @children
        
        // Size monitoring logic would be implemented here
        // to emit size-changed callback when dimensions change
    }
}
```

### 22. Drag and Drop Visual Feedback

COSMIC drag and drop styling:

```slint
export component CosmicDragPreview {
    in property <image> preview-image;
    in property <string> preview-text;
    
    opacity: 0.7;
    
    Rectangle {
        background: CosmicPalette.surface-primary;
        border-radius: CosmicCornerRadii.radius-s;
        drop-shadow-blur: 8px;
        drop-shadow-color: CosmicPalette.shadow;
        
        HorizontalLayout {
            padding: CosmicSpacing.xs;
            spacing: CosmicSpacing.xs;
            
            if preview-image != @image-url(""): Image {
                source: preview-image;
                width: 24px;
                height: 24px;
            }
            
            Text {
                text: preview-text;
                color: CosmicPalette.text-primary;
            }
        }
    }
}

export component CosmicDropTarget {
    in property <bool> drag-over;
    in property <bool> drop-accepted;
    
    states [
        drag-over when root.drag-over: {
            background: CosmicPalette.accent-primary.with-alpha(0.1);
            border-color: CosmicPalette.accent-primary;
            border-width: 2px;
        }
    ]
}
```

This comprehensive design specification provides detailed component patterns based on the COSMIC framework, adapted for Slint implementation. Each component includes proper state management, callbacks, and styling that matches COSMIC Desktop's design language.



## Data Models

### Theme Configuration Model

```rust
pub struct CosmicTheme {
    pub mode: ThemeMode,
    pub accent_color: AccentColor,
    pub custom_accent: Option<Color>,
}

pub enum ThemeMode {
    Light,
    Dark,
    Auto, // Follow system preference
}

pub enum AccentColor {
    Brownish,  // #8B7355
    Orange,    // #FF6B35
    Custom,
}

pub struct Color {
    pub r: u8,
    pub g: u8,
    pub b: u8,
    pub a: u8,
}
```

### Component State Models

```rust
pub enum ButtonState {
    Normal,
    Hover,
    Pressed,
    Focused,
    Disabled,
    Loading,
}

pub enum InputState {
    Normal,
    Focused,
    Error,
    Disabled,
}

pub struct ValidationError {
    pub field: String,
    pub message: String,
}
```

### Layout Models

```rust
pub struct SpacingScale {
    pub xxxs: f32,  // 4px
    pub xxs: f32,   // 8px
    pub xs: f32,    // 12px
    pub s: f32,     // 16px
    pub m: f32,     // 24px
    pub l: f32,     // 32px
    pub xl: f32,    // 48px
    pub xxl: f32,   // 64px
}

pub struct CornerRadii {
    pub radius_0: f32,    // 0px
    pub radius_xs: f32,   // 4px
    pub radius_s: f32,    // 8px
    pub radius_m: f32,    // 12px
    pub radius_l: f32,    // 16px
    pub radius_xl: f32,   // 20px
    pub radius_xxl: f32,  // 24px
    pub radius_full: f32, // 9999px
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Theme Consistency Across Components
*For any* component in the system, when the theme mode changes from light to dark (or vice versa), all color properties should update to use the corresponding theme palette values.
**Validates: Requirements 1.1, 1.2, 13.1-13.5, 14.1-14.5**

### Property 2: Accent Color Propagation
*For any* interactive element (button, link, toggle, slider), when the accent color is changed in the theme configuration, the element should reflect the new accent color in its active, hover, and selected states.
**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

### Property 3: Typography Scale Consistency
*For any* text element, the font size used should be one of the defined typography scale values (12px, 14px, 16px, 18px, 20px, 24px, 32px), ensuring consistent text hierarchy.
**Validates: Requirements 3.5, 25.1-25.8**

### Property 4: Spacing Scale Adherence
*For any* layout with padding or margins, the spacing values should be multiples of 4px from the defined spacing scale (4px, 8px, 12px, 16px, 24px, 32px, 48px, 64px).
**Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 37.1-37.8**

### Property 5: Border Radius Consistency
*For any* component with rounded corners, the border radius should be one of the defined values from the corner radii system (0px, 4px, 8px, 12px, 16px, 20px, 24px, or 9999px).
**Validates: Requirements 4.1-4.5, 38.1-38.8**

### Property 6: Focus Indicator Visibility
*For any* focusable element, when it receives keyboard focus, a 2px accent-colored outline should be visible with sufficient contrast against the background.
**Validates: Requirements 26.1, 26.2, 54.1-54.7**

### Property 7: Disabled State Opacity
*For any* interactive component, when disabled, the opacity should be reduced to 40% and all interaction callbacks should be prevented.
**Validates: Requirements 7.4, 8.5, 34.5, 56.5, 57.7, 58.6, 59.8, 60.8**

### Property 8: Button State Transitions
*For any* button, state transitions (normal → hover → pressed → focused) should animate smoothly over 150-200ms with appropriate visual feedback.
**Validates: Requirements 7.5, 7.8, 34.1-34.7**

### Property 9: Input Validation Error Display
*For any* input field with validation errors, the field should display a red border (2px) and show the error message below the field in 12px font.
**Validates: Requirements 8.5, 56.4**

### Property 10: List Item Selection Indication
*For any* selectable list item, when selected, it should display an accent color background at 10-15% opacity and show a checkmark icon.
**Validates: Requirements 9.3, 27.1, 27.4**

### Property 11: Modal Dialog Focus Trap
*For any* modal dialog, when opened, keyboard focus should be trapped within the dialog and return to the trigger element when closed.
**Validates: Requirements 26.4, 26.5, 65.6**

### Property 12: Responsive Navigation Collapse
*For any* navigation sidebar, when the window width falls below the collapse threshold, the navigation should hide text labels and show only icons.
**Validates: Requirements 20.5, 28.1-28.7, 41.7, 50.5**

### Property 13: Accessibility Contrast Ratio
*For any* text on a background, the contrast ratio should meet WCAG 2.1 AA standards (4.5:1 for normal text, 3:1 for large text).
**Validates: Requirements 19.1, 19.4**

### Property 14: Keyboard Navigation Sequence
*For any* view with multiple interactive elements, pressing Tab should move focus to the next element in logical reading order, and Shift+Tab should move to the previous element.
**Validates: Requirements 19.5, 53.1, 53.2**

### Property 15: Popover Viewport Containment
*For any* popover or dropdown, when opened near viewport edges, the position should be adjusted to ensure the entire popover remains visible within the viewport.
**Validates: Requirements 24.4, 24.5, 46.4, 64.4**

### Property 16: Animation Duration Consistency
*For any* animated transition (hover, focus, expand, slide), the animation duration should be between 150ms and 300ms with appropriate easing functions.
**Validates: Requirements 7.8, 34.6, 58.4, 66.2, 67.3**

### Property 17: Icon Size Consistency
*For any* icon displayed in the UI, the size should be one of the standard sizes: 12px (small), 16px (standard), 20px (navigation), 24px (large), or 32px+ (hero).
**Validates: Requirements 12.3, 12.4, 30.1, 30.2**

### Property 18: Shadow Elevation Hierarchy
*For any* elevated surface, the shadow should correspond to its elevation level: 1-2px blur for cards, 4-8px for dialogs, 8-16px for popovers.
**Validates: Requirements 5.1, 5.2, 5.3, 5.4**

### Property 19: Toggle State Synchronization
*For any* toggle switch, the visual state (knob position, background color) should always match the boolean value it represents.
**Validates: Requirements 58.1-58.4**

### Property 20: Table Row Zebra Striping
*For any* data table with zebra striping enabled, alternating rows should have background colors that differ by 3-5% opacity.
**Validates: Requirements 9.1, 71.1**

## Error Handling

### Theme Loading Errors

**Error Scenario**: Theme configuration file is missing or corrupted
- **Handling**: Fall back to default COSMIC Dark theme
- **User Feedback**: Show warning notification: "Theme configuration unavailable, using default theme"
- **Recovery**: Attempt to regenerate default theme configuration

**Error Scenario**: Invalid accent color value in configuration
- **Handling**: Use default brownish accent (#8B7355)
- **User Feedback**: Log warning to console
- **Recovery**: Validate and sanitize color values on next save

### Component Rendering Errors

**Error Scenario**: Required icon file is missing
- **Handling**: Display placeholder icon or text label
- **User Feedback**: Log error with missing icon path
- **Recovery**: Provide fallback icon set in component library

**Error Scenario**: Font file fails to load
- **Handling**: Fall back to system default sans-serif font
- **User Feedback**: Show notification: "Custom font unavailable, using system font"
- **Recovery**: Retry font loading on next application start

### Input Validation Errors

**Error Scenario**: User enters invalid data in form field
- **Handling**: Display red border and error message below field
- **User Feedback**: Show specific validation error (e.g., "Email address is invalid")
- **Recovery**: Allow user to correct input, clear error on valid input

**Error Scenario**: Required field is empty on form submission
- **Handling**: Prevent submission, highlight all invalid fields
- **User Feedback**: Show error summary at top of form
- **Recovery**: Focus first invalid field for correction

### Responsive Layout Errors

**Error Scenario**: Window size becomes too small for minimum layout
- **Handling**: Enforce minimum window size (320px width)
- **User Feedback**: Prevent window from being resized below minimum
- **Recovery**: Adjust layout to fit minimum size constraints

**Error Scenario**: Navigation sidebar cannot collapse properly
- **Handling**: Hide sidebar completely and show hamburger menu
- **User Feedback**: Smooth transition to mobile layout
- **Recovery**: Restore sidebar when window size increases

### Accessibility Errors

**Error Scenario**: Contrast ratio falls below WCAG standards
- **Handling**: Automatically adjust text color to meet minimum contrast
- **User Feedback**: Log warning about contrast adjustment
- **Recovery**: Provide theme customization to fix contrast issues

**Error Scenario**: Focus indicator is not visible
- **Handling**: Use high-contrast outline color (accent or white/black)
- **User Feedback**: Ensure outline is always visible
- **Recovery**: Test focus indicators against all background colors

## Testing Strategy

### Unit Testing Approach

**Component Rendering Tests**
- Test each component renders correctly with default props
- Test component renders correctly with all prop variations
- Test component handles missing optional props gracefully
- Test component applies correct styles based on theme mode
- Test component responds to prop changes reactively

**State Management Tests**
- Test button state transitions (normal → hover → pressed → focused → disabled)
- Test input field state changes (normal → focused → error)
- Test toggle switch state synchronization with boolean value
- Test dialog open/close state management
- Test navigation active item state

**Theme System Tests**
- Test theme mode switching (light ↔ dark)
- Test accent color changes propagate to all components
- Test color palette values are correct for each mode
- Test typography scale values are applied correctly
- Test spacing scale values are used consistently

**Example Unit Test (Rust + Slint)**:
```rust
#[test]
fn test_button_disabled_state() {
    let button = CosmicButton::new();
    button.set_enabled(false);
    
    assert_eq!(button.get_opacity(), 0.4);
    assert_eq!(button.get_cursor(), "not-allowed");
    
    // Attempt to click disabled button
    button.click();
    assert_eq!(button.get_click_count(), 0); // Should not increment
}
```

### Property-Based Testing Approach

**Testing Framework**: Use `proptest` crate for Rust property-based testing

**Property Test Configuration**: Each property test should run a minimum of 100 iterations to ensure comprehensive coverage of the input space.

**Property Test 1: Theme Consistency**
```rust
use proptest::prelude::*;

proptest! {
    #[test]
    fn prop_theme_consistency_across_components(
        theme_mode in prop::bool::ANY,
        component_type in 0..10usize
    ) {
        // **Feature: cosmic-desktop-styling, Property 1: Theme Consistency Across Components**
        let theme = CosmicTheme::new(if theme_mode { ThemeMode::Dark } else { ThemeMode::Light });
        let component = create_component(component_type, &theme);
        
        let expected_bg = if theme_mode { 
            theme.dark_background 
        } else { 
            theme.light_background 
        };
        
        prop_assert_eq!(component.get_background_color(), expected_bg);
    }
}
```

**Property Test 2: Accent Color Propagation**
```rust
proptest! {
    #[test]
    fn prop_accent_color_propagation(
        r in 0u8..=255,
        g in 0u8..=255,
        b in 0u8..=255,
        component_type in 0..5usize
    ) {
        // **Feature: cosmic-desktop-styling, Property 2: Accent Color Propagation**
        let accent = Color::rgb(r, g, b);
        let theme = CosmicTheme::with_accent(accent);
        let component = create_interactive_component(component_type, &theme);
        
        component.set_state(ComponentState::Active);
        prop_assert_eq!(component.get_accent_color(), accent);
    }
}
```

**Property Test 3: Typography Scale Consistency**
```rust
proptest! {
    #[test]
    fn prop_typography_scale_consistency(
        text_content in "\\PC{1,100}",
        text_level in 0..7usize
    ) {
        // **Feature: cosmic-desktop-styling, Property 3: Typography Scale Consistency**
        let valid_sizes = vec![12.0, 14.0, 16.0, 18.0, 20.0, 24.0, 32.0];
        let text = CosmicText::new(text_content, text_level);
        
        prop_assert!(valid_sizes.contains(&text.get_font_size()));
    }
}
```

**Property Test 4: Spacing Scale Adherence**
```rust
proptest! {
    #[test]
    fn prop_spacing_scale_adherence(
        container_type in 0..8usize,
        content_count in 1..20usize
    ) {
        // **Feature: cosmic-desktop-styling, Property 4: Spacing Scale Adherence**
        let container = create_container(container_type, content_count);
        let padding = container.get_padding();
        let margin = container.get_margin();
        
        // All spacing should be multiples of 4px
        prop_assert_eq!(padding % 4.0, 0.0);
        prop_assert_eq!(margin % 4.0, 0.0);
    }
}
```

**Property Test 5: Border Radius Consistency**
```rust
proptest! {
    #[test]
    fn prop_border_radius_consistency(
        component_type in 0..15usize
    ) {
        // **Feature: cosmic-desktop-styling, Property 5: Border Radius Consistency**
        let valid_radii = vec![0.0, 4.0, 8.0, 12.0, 16.0, 20.0, 24.0, 9999.0];
        let component = create_component_with_corners(component_type);
        
        prop_assert!(valid_radii.contains(&component.get_border_radius()));
    }
}
```

**Property Test 6: Focus Indicator Visibility**
```rust
proptest! {
    #[test]
    fn prop_focus_indicator_visibility(
        component_type in 0..20usize,
        bg_r in 0u8..=255,
        bg_g in 0u8..=255,
        bg_b in 0u8..=255
    ) {
        // **Feature: cosmic-desktop-styling, Property 6: Focus Indicator Visibility**
        let background = Color::rgb(bg_r, bg_g, bg_b);
        let component = create_focusable_component(component_type, background);
        
        component.focus();
        
        let outline = component.get_focus_outline();
        prop_assert_eq!(outline.width, 2.0);
        
        // Ensure sufficient contrast
        let contrast = calculate_contrast(outline.color, background);
        prop_assert!(contrast >= 3.0);
    }
}
```

**Property Test 7: Disabled State Opacity**
```rust
proptest! {
    #[test]
    fn prop_disabled_state_opacity(
        component_type in 0..10usize
    ) {
        // **Feature: cosmic-desktop-styling, Property 7: Disabled State Opacity**
        let component = create_interactive_component(component_type);
        component.set_enabled(false);
        
        prop_assert_eq!(component.get_opacity(), 0.4);
        
        // Attempt interaction
        let clicked = component.try_click();
        prop_assert!(!clicked); // Should not respond to clicks
    }
}
```

**Property Test 8: Input Validation Error Display**
```rust
proptest! {
    #[test]
    fn prop_input_validation_error_display(
        input_value in ".*",
        error_message in "\\PC{1,100}"
    ) {
        // **Feature: cosmic-desktop-styling, Property 9: Input Validation Error Display**
        let input = CosmicTextInput::new();
        input.set_value(input_value);
        input.set_error(error_message.clone());
        
        prop_assert_eq!(input.get_border_color(), Color::rgb(231, 76, 60)); // Error red
        prop_assert_eq!(input.get_border_width(), 2.0);
        prop_assert_eq!(input.get_error_message(), error_message);
        prop_assert_eq!(input.get_error_font_size(), 12.0);
    }
}
```

**Property Test 9: Accessibility Contrast Ratio**
```rust
proptest! {
    #[test]
    fn prop_accessibility_contrast_ratio(
        text_size in 12.0..32.0f32,
        bg_r in 0u8..=255,
        bg_g in 0u8..=255,
        bg_b in 0u8..=255
    ) {
        // **Feature: cosmic-desktop-styling, Property 13: Accessibility Contrast Ratio**
        let background = Color::rgb(bg_r, bg_g, bg_b);
        let text = CosmicText::with_background(text_size, background);
        
        let text_color = text.get_color();
        let contrast = calculate_contrast(text_color, background);
        
        let min_contrast = if text_size >= 18.0 { 3.0 } else { 4.5 };
        prop_assert!(contrast >= min_contrast);
    }
}
```

**Property Test 10: Popover Viewport Containment**
```rust
proptest! {
    #[test]
    fn prop_popover_viewport_containment(
        trigger_x in 0.0..1920.0f32,
        trigger_y in 0.0..1080.0f32,
        popover_width in 100.0..600.0f32,
        popover_height in 100.0..400.0f32
    ) {
        // **Feature: cosmic-desktop-styling, Property 15: Popover Viewport Containment**
        let viewport = Rect::new(0.0, 0.0, 1920.0, 1080.0);
        let trigger = Point::new(trigger_x, trigger_y);
        
        let popover = CosmicPopover::new(popover_width, popover_height);
        let position = popover.calculate_position(trigger, viewport);
        
        // Popover should be fully within viewport
        prop_assert!(position.x >= 0.0);
        prop_assert!(position.y >= 0.0);
        prop_assert!(position.x + popover_width <= viewport.width);
        prop_assert!(position.y + popover_height <= viewport.height);
    }
}
```

### Integration Testing

**Theme Switching Integration Test**
- Test switching from light to dark mode updates all visible components
- Test accent color change propagates to all interactive elements
- Test theme persistence across application restarts

**Navigation Flow Integration Test**
- Test navigation between all application views
- Test active navigation item updates correctly
- Test navigation state persists during view transitions

**Form Submission Integration Test**
- Test form validation across all input types
- Test error messages display correctly
- Test successful submission clears form and shows confirmation

**Responsive Behavior Integration Test**
- Test layout adapts correctly at various window sizes
- Test navigation collapses at narrow widths
- Test dialogs and popovers reposition correctly

### Visual Regression Testing

**Approach**: Use screenshot comparison testing to detect unintended visual changes

**Test Scenarios**:
- Capture screenshots of each component in all states (normal, hover, focused, disabled)
- Capture screenshots in both light and dark modes
- Capture screenshots at different window sizes (mobile, tablet, desktop)
- Compare against baseline screenshots to detect regressions

**Tools**: Consider using `slint-viewer` with automated screenshot capture

### Accessibility Testing

**Keyboard Navigation Testing**
- Test Tab/Shift+Tab navigation through all interactive elements
- Test Enter/Space activation of buttons and controls
- Test Escape key closes dialogs and popovers
- Test arrow key navigation in menus and lists

**Screen Reader Testing**
- Test all components have appropriate ARIA labels
- Test focus announcements are clear and descriptive
- Test state changes are announced to screen readers

**Contrast Testing**
- Test all text meets WCAG 2.1 AA contrast requirements
- Test focus indicators are visible against all backgrounds
- Test color is not the only means of conveying information

### Performance Testing

**Rendering Performance**
- Test component render time is under 16ms (60fps)
- Test theme switching completes within 200ms
- Test large lists (1000+ items) render smoothly with virtual scrolling

**Memory Usage**
- Test application memory usage remains stable during extended use
- Test no memory leaks when opening/closing dialogs repeatedly
- Test theme switching doesn't cause memory accumulation

## Implementation Notes

### Migration Path

1. **Create cosmic-1.0 directory structure** alongside existing material-1.0
2. **Implement foundation** (palette, typography, spacing, elevation)
3. **Implement core components** (buttons, inputs, cards) with full test coverage
4. **Implement layout components** (app bar, drawer, dialogs)
5. **Update one view at a time** to use cosmic-1.0 components
6. **Test each view** thoroughly before moving to the next
7. **Remove material-1.0** once all views are migrated

### Slint-Specific Considerations

**Property Bindings**: Use Slint's reactive property system for theme changes
```slint
background: CosmicPalette.background; // Automatically updates when theme changes
```

**Callbacks**: Define clear callback signatures for all interactive components
```slint
callback clicked();
callback value-changed(string);
callback selection-changed(int);
```

**States**: Use Slint's state system for component variants
```slint
states [
    hover when touch-area.has-hover: {
        background: CosmicPalette.surface-hover;
    }
    pressed when touch-area.pressed: {
        background: CosmicPalette.surface-pressed;
    }
]
```

**Animations**: Use Slint's animation system for smooth transitions
```slint
animate background { duration: 150ms; easing: ease-out; }
```

### Accessibility Implementation

**Focus Management**
- Ensure all interactive elements are keyboard accessible
- Implement focus trap for modal dialogs
- Provide skip links for keyboard navigation

**ARIA Attributes**
- Add appropriate role attributes to custom components
- Provide aria-label for icon-only buttons
- Use aria-describedby for error messages

**Color Independence**
- Never rely solely on color to convey information
- Provide icons or text labels alongside color indicators
- Ensure sufficient contrast for all text

### Performance Optimization

**Lazy Loading**
- Load components only when needed
- Defer loading of heavy components until visible
- Use virtual scrolling for long lists

**Memoization**
- Cache computed color values
- Memoize expensive layout calculations
- Reuse component instances where possible

**Efficient Updates**
- Batch theme updates to minimize repaints
- Use Slint's reactive system to update only changed properties
- Avoid unnecessary re-renders

This comprehensive design provides a complete blueprint for implementing the COSMIC Desktop styling system in Slint, with detailed specifications for all 74 requirements, robust testing strategies, and clear implementation guidance.
