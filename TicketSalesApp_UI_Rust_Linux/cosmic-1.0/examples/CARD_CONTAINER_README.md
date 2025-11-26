# Card and Container Components

This document describes the COSMIC Desktop card, container, and divider components.

## Components

### CosmicCard

A card component for displaying grouped content with elevation and optional interactivity.

**Requirements:** 18.1-18.5, 48.1-48.7

**Properties:**
- `title: string` - Optional card title displayed in header
- `interactive: bool` - Whether the card responds to hover and click (default: false)
- `layer: CardLayer` - Background layer (background, primary, secondary)
- `elevated: bool` - Whether to show shadow (default: true)
- `has-border: bool` - Whether to show border (default: false)
- `padding-size: CardPadding` - Padding density (compact: 16px, standard: 24px, comfortable: 32px)

**Callbacks:**
- `clicked()` - Emitted when interactive card is clicked

**Features:**
- Elevated surface with subtle shadow (Requirement 18.1)
- Title with medium font weight (Requirement 18.2)
- Content area with 16-20px padding (Requirement 18.3)
- Interactive variant with hover effects (Requirement 18.4)
- Border radius 12-16px (Requirement 18.5, 4.2)
- Increases shadow on hover (sm to md) (Requirement 18.4)
- Layer-based background colors (Requirements 48.1-48.4)
- Subtle shadows with 10-20% opacity (Requirement 48.5)
- Optional 1px neutral border (Requirement 48.6)
- Hover state with background change (Requirement 48.7)

**Usage:**

```slint
import { CosmicCard, CardLayer, CardPadding } from "../ui/components/card.slint";

// Basic card with title
CosmicCard {
    title: "My Card";
    
    Text {
        text: "Card content goes here";
    }
}

// Interactive card
CosmicCard {
    title: "Click Me";
    interactive: true;
    
    clicked => {
        debug("Card clicked!");
    }
    
    Text {
        text: "This card responds to hover and click";
    }
}

// Card with custom padding and layer
CosmicCard {
    title: "Custom Card";
    padding-size: CardPadding.comfortable;
    layer: CardLayer.primary;
    has-border: true;
    
    Text {
        text: "Card on primary layer with border";
    }
}
```

### CosmicContainer

A generic container component with padding and background layer support.

**Requirements:** 35.1-35.7

**Properties:**
- `layer: ContainerLayer` - Background layer (background, primary, secondary)
- `has-border: bool` - Whether to show border (default: false)
- `padding-density: ContainerPadding` - Padding density (compact: 16px, standard: 24px)
- `elevated: bool` - Whether to show shadow (default: false)
- `custom-padding: length` - Override padding with custom value (default: -1px, disabled)
- `border-radius: length` - Border radius (default: 0px)

**Features:**
- Appropriate background for current layer (Requirement 35.1)
- Background layer uses background.base color (Requirement 35.2)
- Primary layer uses primary.base color (Requirement 35.3)
- Secondary layer uses secondary.base color (Requirement 35.4)
- Optional 1px neutral border (Requirement 35.5)
- 16-24px padding based on content density (Requirement 35.6)
- Optional subtle shadow with 10-20% opacity (Requirement 35.7)

**Usage:**

```slint
import { CosmicContainer, ContainerLayer, ContainerPadding } from "../ui/components/container.slint";

// Basic container
CosmicContainer {
    layer: ContainerLayer.background;
    
    Text {
        text: "Container content";
    }
}

// Container with border and elevation
CosmicContainer {
    layer: ContainerLayer.primary;
    has-border: true;
    elevated: true;
    border-radius: 12px;
    
    Text {
        text: "Elevated container with border";
    }
}

// Container with custom padding
CosmicContainer {
    layer: ContainerLayer.secondary;
    custom-padding: 32px;
    
    Text {
        text: "Container with custom padding";
    }
}
```

### CosmicDivider

A horizontal or vertical separator for content sections.

**Requirements:** 36.1-36.6

**Properties:**
- `orientation: Orientation` - Horizontal or vertical (default: horizontal)
- `label: string` - Optional label centered in divider
- `label-spacing: length` - Spacing around label (default: 16px)
- `margin-left: length` - Left margin for list dividers (default: 0px)
- `margin-right: length` - Right margin for list dividers (default: 0px)

**Features:**
- Horizontal separator with 1px height (Requirement 36.1)
- Vertical separator with 1px width (Requirement 36.2)
- Adjusts color for light mode (Requirement 36.3)
- Adjusts color for dark mode (Requirement 36.4)
- Optional label centered with spacing (Requirement 36.5)
- Spans full width with margins for lists (Requirement 36.6)

**Usage:**

```slint
import { CosmicDivider, Orientation } from "../ui/components/divider.slint";

// Simple horizontal divider
CosmicDivider {
    orientation: Orientation.horizontal;
}

// Divider with label
CosmicDivider {
    orientation: Orientation.horizontal;
    label: "Section Title";
}

// Vertical divider
CosmicDivider {
    orientation: Orientation.vertical;
}

// List divider with margins
CosmicDivider {
    orientation: Orientation.horizontal;
    margin-left: 16px;
    margin-right: 16px;
}
```

## Enums

### CardLayer
- `background` - On background layer, uses component hover color
- `primary` - On primary layer, uses primary component colors
- `secondary` - On secondary layer, uses secondary component colors

### CardPadding
- `compact` - 16px padding
- `standard` - 24px padding (default)
- `comfortable` - 32px padding

### ContainerLayer
- `background` - Background layer, uses background.base color
- `primary` - Primary layer, uses primary.base color
- `secondary` - Secondary layer, uses secondary.base color

### ContainerPadding
- `compact` - 16px padding
- `standard` - 24px padding (default)
- `comfortable` - 24px padding

### Orientation
- `horizontal` - Horizontal divider
- `vertical` - Vertical divider

## Running the Example

To run the card and container example:

```bash
cd TicketSalesApp_UI_Rust_Linux
slint-viewer cosmic-1.0/examples/card-container-example.slint
```

Or if using the Rust build:

```bash
cargo run --example card-container-example
```

## Design Notes

### Cards vs Containers

**Use Cards when:**
- Displaying grouped, related content
- Content needs visual separation with elevation
- Interactive elements that respond to hover/click
- Content has a clear title or header

**Use Containers when:**
- Simple content grouping without elevation
- Background color differentiation is needed
- Flexible padding and border options are required
- Building custom layouts with consistent spacing

### Layer System

The layer system provides consistent background colors across components:

- **Background Layer**: Base application background
- **Primary Layer**: Elevated surfaces (cards, dialogs)
- **Secondary Layer**: Further elevated surfaces (nested cards)

This creates a visual hierarchy that helps users understand content organization.

### Elevation

Cards use subtle shadows (10-20% opacity) to create depth:
- Normal state: Small shadow (1-2px offset)
- Hover state (interactive cards): Medium shadow (2-4px offset)

Containers can optionally use elevation, but default to flat appearance.

## Accessibility

All components follow COSMIC Desktop accessibility guidelines:

- Minimum contrast ratio of 4.5:1 for text
- Interactive cards have clear hover states
- Focus indicators on interactive elements
- Semantic color usage with proper contrast
- Support for light and dark modes

## Theme Integration

All components automatically adapt to:
- Light/dark mode changes
- Accent color configuration
- Custom spacing scales
- Border radius preferences

Use `CosmicPalette.dark-mode` to toggle between light and dark themes.
