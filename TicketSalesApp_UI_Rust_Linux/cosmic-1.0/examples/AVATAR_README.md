# Avatar Components

This document describes the COSMIC Desktop avatar components implementation.

## Components

### CosmicAvatar

A user representation component that displays profile images or initials.

**Requirements:** 63.1-63.10

**Features:**
- Circular shape by default (Requirement 63.1)
- Four size variants: xs (24px), sm (32px), md (40px), lg (56px) (Requirements 63.2-63.5)
- Initials fallback with colored background when no image is provided (Requirement 63.6)
- Status indicator dot at bottom-right corner (Requirement 63.7)
- Square variant with 8px border radius (Requirement 63.10)
- Clickable variant with hover border effect (Requirement 63.9)

**Properties:**
- `avatar-image: image` - Profile image to display
- `initials: string` - Fallback initials (e.g., "JD" for John Doe)
- `size: AvatarSize` - Size variant (xs, sm, md, lg)
- `shape: AvatarShape` - Shape variant (circle, square)
- `status: AvatarStatus` - Status indicator (none, online, away, busy, offline)
- `clickable: bool` - Enable click interaction with hover border
- `fallback-color: color` - Background color for initials display

**Callbacks:**
- `clicked()` - Emitted when clickable avatar is clicked

**Example:**
```slint
CosmicAvatar {
    size: AvatarSize.md;
    initials: "JD";
    fallback-color: #8B7355;
    status: AvatarStatus.online;
    clickable: true;
    
    clicked => {
        debug("Avatar clicked!");
    }
}
```

### CosmicAvatarGroup

Displays multiple avatars in an overlapping layout with overflow handling.

**Requirements:** 63.8

**Features:**
- Overlapping avatars with -8px margin (Requirement 63.8)
- Overflow count display (e.g., "+3") when exceeding max visible
- Click events on individual avatars (Requirement 63.8)
- Configurable maximum visible avatars
- Support for all avatar sizes and shapes

**Properties:**
- `avatars: [AvatarData]` - Array of avatar data
- `size: AvatarSize` - Size for all avatars in group
- `shape: AvatarShape` - Shape for all avatars in group
- `max-visible: int` - Maximum avatars to show before overflow (default: 5)
- `clickable: bool` - Enable click interaction on avatars

**Callbacks:**
- `avatar-clicked(int)` - Emitted when avatar is clicked (index of avatar, or -1 for overflow)

**AvatarData Structure:**
```slint
{
    image: image,
    initials: string,
    fallback-color: color,
    status: AvatarStatus,
}
```

**Example:**
```slint
CosmicAvatarGroup {
    avatars: [
        { initials: "AB", fallback-color: #8B7355, status: AvatarStatus.online },
        { initials: "CD", fallback-color: #FF6B35, status: AvatarStatus.away },
        { initials: "EF", fallback-color: #3498DB, status: AvatarStatus.none },
        { initials: "GH", fallback-color: #27AE60, status: AvatarStatus.busy },
        { initials: "IJ", fallback-color: #E74C3C, status: AvatarStatus.offline },
        { initials: "KL", fallback-color: #F39C12, status: AvatarStatus.none },
    ];
    size: AvatarSize.md;
    max-visible: 4;
    clickable: true;
    
    avatar-clicked(index) => {
        if index == -1 {
            debug("Overflow clicked - show all members");
        } else {
            debug("Avatar " + index + " clicked");
        }
    }
}
```

## Enums

### AvatarSize
- `xs` - 24px (Requirement 63.2)
- `sm` - 32px (Requirement 63.3)
- `md` - 40px (Requirement 63.4)
- `lg` - 56px (Requirement 63.5)

### AvatarShape
- `circle` - Circular shape (default) (Requirement 63.1)
- `square` - Square with 8px border radius (Requirement 63.10)

### AvatarStatus
- `none` - No status indicator
- `online` - Green dot
- `away` - Yellow dot
- `busy` - Red dot
- `offline` - Gray dot

## Design Specifications

### Sizes
- **Extra Small (xs):** 24px × 24px, 10px font, 6px status dot
- **Small (sm):** 32px × 32px, 12px font, 8px status dot
- **Medium (md):** 40px × 40px, 16px font, 10px status dot
- **Large (lg):** 56px × 56px, 20px font, 12px status dot

### Border Radius
- **Circle:** 9999px (fully rounded)
- **Square:** 8px

### Status Indicator
- Positioned at bottom-right corner (Requirement 63.7)
- 2px white border to separate from avatar
- Colors match semantic palette (success, warning, error, text-secondary)

### Hover State (Clickable)
- 2px accent color border on hover (Requirement 63.9)
- Smooth 150ms transition
- Pointer cursor

### Avatar Group Overlap
- -8px margin between avatars (Requirement 63.8)
- Later avatars render on top
- Overflow indicator shows "+N" count
- Overflow indicator uses surface-variant background

## Usage Guidelines

1. **Profile Display:** Use medium (md) size for user profiles in lists or cards
2. **Navigation:** Use small (sm) size for compact navigation or headers
3. **Status Indicators:** Show online/away/busy status for messaging or collaboration apps
4. **Groups:** Use avatar groups to show team members, participants, or collaborators
5. **Clickable:** Enable clickable for profile navigation or selection interactions
6. **Initials:** Use 1-2 character initials (e.g., "JD", "AB") for best appearance
7. **Colors:** Use distinct fallback colors to differentiate users without images

## Running the Example

To see the avatar components in action:

```bash
slint-viewer examples/avatar-example.slint
```

The example demonstrates:
- All size variants (xs, sm, md, lg)
- Both shape variants (circle, square)
- All status indicators (online, away, busy, offline)
- Clickable avatars with hover effects
- Avatar groups with different configurations
- Overflow handling in large groups
