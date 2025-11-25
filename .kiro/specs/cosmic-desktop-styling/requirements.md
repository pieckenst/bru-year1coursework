# Requirements Document

## Introduction

This specification defines the requirements for transitioning the Rust/Slint-based Linux application from Material Design styling to COSMIC Desktop styling. COSMIC Desktop is the modern desktop environment developed by System76 for Pop!_OS, featuring a clean, minimalist aesthetic with excellent light/dark mode support and thoughtful use of color and spacing. This redesign will update the visual appearance of the application while maintaining all existing functionality.Implementation should be complex , and not simplified

## Glossary

- **System**: The Rust/Slint-based Linux application for BRU Avtopark
- **COSMIC Desktop**: The desktop environment developed by System76, featuring modern, minimalist design principles
- **Component Library**: The collection of reusable UI components in the material-1.0 directory
- **Theme**: A collection of colors, typography, spacing, and visual styles that define the application's appearance
- **Accent Color**: The primary brand color used for interactive elements, buttons, and highlights
- **Neutral Background**: Subtle gray or beige backgrounds used for containers and surfaces
- **Light Mode**: The light color scheme with light backgrounds and dark text
- **Dark Mode**: The dark color scheme with dark backgrounds and light text
- **Border Radius**: The roundness of corners on UI elements
- **Elevation**: The visual depth created by shadows and layering
- **Typography Scale**: The hierarchy of text sizes and weights used throughout the application

## Requirements

### Requirement 1: COSMIC Desktop Color Palette

**User Story:** As a user, I want the application to use COSMIC Desktop's color palette, so that it feels native to my desktop environment.

#### Acceptance Criteria

1. WHEN the application renders in light mode THEN the system SHALL use light neutral backgrounds (#F5F5F5 to #E8E8E8 range) for surfaces
2. WHEN the application renders in dark mode THEN the system SHALL use dark neutral backgrounds (#2B2B2B to #1A1A1A range) for surfaces
3. WHEN interactive elements are displayed THEN the system SHALL use the defined accent color for buttons, links, and highlights
4. WHEN text is displayed THEN the system SHALL use high-contrast text colors appropriate for the current mode (near-black for light mode, near-white for dark mode)
5. WHEN borders and dividers are displayed THEN the system SHALL use subtle border colors that provide separation without harsh contrast

### Requirement 2: Accent Color Configuration

**User Story:** As a user, I want the application to use an appropriate accent color, so that interactive elements are visually distinct and appealing.

#### Acceptance Criteria

1. WHEN the accent color is defined THEN the system SHALL support either brownish (#8B7355 or similar) or orange (#FF6B35 or similar) as the primary accent
2. WHEN buttons are displayed THEN the system SHALL use the accent color for primary action buttons
3. WHEN the user hovers over interactive elements THEN the system SHALL display a lighter or darker shade of the accent color
4. WHEN elements are in a selected or active state THEN the system SHALL use the accent color to indicate the state
5. WHEN the accent color is used on backgrounds THEN the system SHALL ensure sufficient contrast with text for accessibility

### Requirement 3: Typography System

**User Story:** As a user, I want clear, readable typography, so that I can easily read and understand the application content.

#### Acceptance Criteria

1. WHEN text is displayed THEN the system SHALL use the Inter font family (already available in material-1.0/font)
2. WHEN headings are displayed THEN the system SHALL use font weights of 600 (SemiBold) or 700 (Bold)
3. WHEN body text is displayed THEN the system SHALL use font weight of 400 (Regular) or 500 (Medium)
4. WHEN small text or captions are displayed THEN the system SHALL use font weight of 400 (Regular) with reduced size
5. WHEN text hierarchy is needed THEN the system SHALL use a consistent scale of font sizes (e.g., 12px, 14px, 16px, 18px, 24px, 32px)

### Requirement 4: Border Radius and Roundness

**User Story:** As a user, I want UI elements to have appropriate roundness, so that the interface feels modern and cohesive.

#### Acceptance Criteria

1. WHEN buttons are displayed THEN the system SHALL use a border radius of 8px to 12px
2. WHEN cards or containers are displayed THEN the system SHALL use a border radius of 12px to 16px
3. WHEN input fields are displayed THEN the system SHALL use a border radius of 6px to 8px
4. WHEN dialogs or modals are displayed THEN the system SHALL use a border radius of 16px to 20px
5. WHEN small elements like badges or chips are displayed THEN the system SHALL use a border radius of 4px to 6px

### Requirement 5: Elevation and Shadows

**User Story:** As a user, I want subtle depth and layering in the interface, so that I can understand the hierarchy of elements.

#### Acceptance Criteria

1. WHEN elevated surfaces are displayed THEN the system SHALL use subtle shadows with low opacity (10-20%)
2. WHEN dialogs or modals are displayed THEN the system SHALL use medium elevation shadows to separate from the background
3. WHEN buttons are hovered THEN the system SHALL increase shadow intensity to indicate interactivity
4. WHEN cards are displayed THEN the system SHALL use minimal elevation (1-2px shadow) for subtle depth
5. WHEN the application is in dark mode THEN the system SHALL adjust shadow colors to work with dark backgrounds

### Requirement 6: Spacing and Layout

**User Story:** As a user, I want consistent spacing throughout the application, so that the interface feels organized and breathable.

#### Acceptance Criteria

1. WHEN UI elements are laid out THEN the system SHALL use a spacing scale based on 4px increments (4px, 8px, 12px, 16px, 24px, 32px, 48px)
2. WHEN padding is applied to containers THEN the system SHALL use 16px to 24px for standard containers
3. WHEN margins are applied between sections THEN the system SHALL use 24px to 32px for visual separation
4. WHEN list items are displayed THEN the system SHALL use 12px to 16px vertical spacing between items
5. WHEN form fields are displayed THEN the system SHALL use 16px to 20px spacing between fields

### Requirement 7: Button Styling

**User Story:** As a user, I want buttons to be clearly identifiable and easy to interact with, so that I can perform actions confidently.

#### Acceptance Criteria

1. WHEN primary buttons are displayed THEN the system SHALL use the accent color background with white text
2. WHEN secondary buttons are displayed THEN the system SHALL use transparent background with accent color border and text
3. WHEN danger buttons (delete) are displayed THEN the system SHALL use red color (#E74C3C or similar) for background or border
4. WHEN buttons are disabled THEN the system SHALL reduce opacity to 40-50% and prevent interaction
5. WHEN buttons are hovered THEN the system SHALL darken the background by 10-15% or show a subtle shadow
6. WHEN icon buttons are displayed THEN the system SHALL use 16-20px icon size with 8px padding
7. WHEN text buttons are displayed THEN the system SHALL use 14px font size with 500 (medium) font weight
8. WHEN button states change THEN the system SHALL use smooth transitions for visual feedback

### Requirement 8: Input Field Styling

**User Story:** As a user, I want input fields to be clear and easy to use, so that I can enter data efficiently.

#### Acceptance Criteria

1. WHEN input fields are displayed THEN the system SHALL use a subtle border (1px) with neutral color
2. WHEN input fields are focused THEN the system SHALL display the accent color border (2px) and remove the neutral border
3. WHEN input fields contain text THEN the system SHALL use appropriate text color and size for readability
4. WHEN input fields have placeholders THEN the system SHALL use reduced opacity text (50-60%) for the placeholder
5. WHEN input fields have errors THEN the system SHALL display a red border and error message below the field

### Requirement 9: List and Table Styling

**User Story:** As a user, I want lists and tables to be easy to scan and read, so that I can find information quickly.

#### Acceptance Criteria

1. WHEN lists are displayed THEN the system SHALL use subtle background alternation (zebra striping) with 3-5% opacity difference
2. WHEN list items are hovered THEN the system SHALL display a subtle background highlight (5-8% opacity)
3. WHEN list items are selected THEN the system SHALL display the accent color background with reduced opacity (10-15%)
4. WHEN table headers are displayed THEN the system SHALL use slightly darker background and medium font weight
5. WHEN table borders are displayed THEN the system SHALL use subtle 1px borders with neutral color

### Requirement 10: Dialog and Modal Styling

**User Story:** As a user, I want dialogs and modals to be clearly separated from the main content, so that I can focus on the task at hand.

#### Acceptance Criteria

1. WHEN dialogs are displayed THEN the system SHALL use a semi-transparent backdrop (40-60% opacity black or dark gray)
2. WHEN dialog content is displayed THEN the system SHALL use the appropriate background color for the current mode with elevated shadow
3. WHEN dialog headers are displayed THEN the system SHALL use larger font size and medium weight with bottom border or spacing
4. WHEN dialog buttons are displayed THEN the system SHALL align them to the right with primary action on the far right
5. WHEN dialogs are displayed THEN the system SHALL use 16px to 24px padding around content

### Requirement 11: Navigation Sidebar Styling

**User Story:** As a user, I want the navigation sidebar to be clear and easy to use, so that I can navigate the application efficiently.

#### Acceptance Criteria

1. WHEN the navigation sidebar is displayed THEN the system SHALL use a slightly different background color than the main content (2-5% darker in light mode, 2-5% lighter in dark mode)
2. WHEN navigation items are displayed THEN the system SHALL use icon + text layout with 12px spacing between icon and text
3. WHEN navigation items are hovered THEN the system SHALL display a subtle background highlight with 50% opacity surface color
4. WHEN navigation items are active THEN the system SHALL display the accent color background with 15% opacity and accent color text with medium (500) font weight
5. WHEN navigation groups are displayed THEN the system SHALL use section headers with smaller, uppercase text
6. WHEN navigation items are displayed THEN the system SHALL use 44px height with 8px border radius
7. WHEN navigation items contain icons THEN the system SHALL use 20-24px icon size aligned vertically with text

### Requirement 12: Icon System

**User Story:** As a user, I want consistent, clear icons throughout the application, so that I can quickly identify actions and features.

#### Acceptance Criteria

1. WHEN icons are displayed THEN the system SHALL use a consistent icon set (Material Icons or similar)
2. WHEN icons are displayed next to text THEN the system SHALL align them vertically centered with the text
3. WHEN icons are used in buttons THEN the system SHALL size them appropriately (16px to 20px for standard buttons)
4. WHEN icons are used in navigation THEN the system SHALL size them at 20px to 24px
5. WHEN icons are displayed THEN the system SHALL use the appropriate color for the context (text color, accent color, or specific state color)

### Requirement 13: Light Mode Implementation

**User Story:** As a user, I want a comfortable light mode, so that I can use the application in bright environments.

#### Acceptance Criteria

1. WHEN light mode is active THEN the system SHALL use #F5F5F5 or similar for the main background
2. WHEN light mode is active THEN the system SHALL use #FFFFFF for elevated surfaces (cards, dialogs)
3. WHEN light mode is active THEN the system SHALL use #E8E8E8 or similar for the navigation sidebar background
4. WHEN light mode is active THEN the system SHALL use #1A1A1A or similar for primary text
5. WHEN light mode is active THEN the system SHALL use #666666 or similar for secondary text

### Requirement 14: Dark Mode Implementation

**User Story:** As a user, I want a comfortable dark mode, so that I can use the application in low-light environments.

#### Acceptance Criteria

1. WHEN dark mode is active THEN the system SHALL use #1A1A1A or similar for the main background
2. WHEN dark mode is active THEN the system SHALL use #2B2B2B or similar for elevated surfaces (cards, dialogs)
3. WHEN dark mode is active THEN the system SHALL use #242424 or similar for the navigation sidebar background
4. WHEN dark mode is active THEN the system SHALL use #F5F5F5 or similar for primary text
5. WHEN dark mode is active THEN the system SHALL use #AAAAAA or similar for secondary text

### Requirement 15: Component Library Migration

**User Story:** As a developer, I want to migrate from material-1.0 to cosmic-1.0 component library, so that the new styling is consistently applied.

#### Acceptance Criteria

1. WHEN the component library is migrated THEN the system SHALL create a new cosmic-1.0 directory structure
2. WHEN components are migrated THEN the system SHALL preserve all existing functionality while updating visual styling
3. WHEN the migration is complete THEN the system SHALL update all imports from material-1.0 to cosmic-1.0
4. WHEN components are updated THEN the system SHALL maintain backward compatibility with existing callbacks and properties
5. WHEN the migration is tested THEN the system SHALL verify that all views render correctly with the new styling

### Requirement 16: App Bar Component

**User Story:** As a user, I want a clean, modern app bar, so that I can see the current view and access global actions.

#### Acceptance Criteria

1. WHEN the app bar is displayed THEN the system SHALL use the appropriate background color for the current mode
2. WHEN the app bar title is displayed THEN the system SHALL use 18px to 20px font size with 600 (semibold) font weight
3. WHEN app bar actions are displayed THEN the system SHALL use icon buttons (40x40px) with 8px border radius and subtle hover effects
4. WHEN the app bar has a shadow THEN the system SHALL use minimal elevation (1-2px) to separate from content
5. WHEN the app bar is displayed THEN the system SHALL use 16px to 20px vertical padding and 24px horizontal padding
6. WHEN app bar action buttons are hovered THEN the system SHALL display surface-variant background color
7. WHEN the app bar contains a menu toggle THEN the system SHALL use navbar-open-symbolic or navbar-closed-symbolic icons

### Requirement 17: Drawer Component

**User Story:** As a user, I want a clean navigation drawer, so that I can access all application features.

#### Acceptance Criteria

1. WHEN the drawer is displayed THEN the system SHALL use the navigation sidebar background color
2. WHEN drawer items are displayed THEN the system SHALL use the navigation item styling defined in Requirement 11
3. WHEN the drawer header is displayed THEN the system SHALL show the application name or logo with appropriate spacing
4. WHEN the drawer is displayed THEN the system SHALL use full height with smooth transitions
5. WHEN drawer sections are displayed THEN the system SHALL use dividers or spacing to separate groups

### Requirement 18: Card Component

**User Story:** As a user, I want cards to display grouped information clearly, so that I can understand related content.

#### Acceptance Criteria

1. WHEN cards are displayed THEN the system SHALL use elevated surface background color with subtle shadow
2. WHEN cards have headers THEN the system SHALL use medium font weight and appropriate spacing
3. WHEN cards have content THEN the system SHALL use 16px to 20px padding around the content
4. WHEN cards are interactive THEN the system SHALL display hover effects (increased shadow or background change)
5. WHEN cards are displayed THEN the system SHALL use the border radius defined in Requirement 4

### Requirement 19: Accessibility Compliance

**User Story:** As a user with accessibility needs, I want the application to meet accessibility standards, so that I can use it effectively.

#### Acceptance Criteria

1. WHEN colors are used THEN the system SHALL ensure a minimum contrast ratio of 4.5:1 for normal text and 3:1 for large text
2. WHEN interactive elements are displayed THEN the system SHALL provide clear focus indicators
3. WHEN colors convey information THEN the system SHALL provide additional non-color indicators (icons, text)
4. WHEN text is displayed THEN the system SHALL use minimum font size of 12px for body text
5. WHEN the application is navigated THEN the system SHALL support keyboard navigation for all interactive elements

### Requirement 20: Responsive Behavior

**User Story:** As a user, I want the application to adapt to different window sizes, so that I can use it comfortably at any size.

#### Acceptance Criteria

1. WHEN the window is resized THEN the system SHALL adjust spacing and layout appropriately
2. WHEN the window is narrow THEN the system SHALL collapse or hide less critical elements
3. WHEN lists are displayed THEN the system SHALL maintain readability at different window widths
4. WHEN dialogs are displayed THEN the system SHALL scale appropriately to fit the window
5. WHEN the navigation is displayed THEN the system SHALL adapt to narrow windows (collapse to icons only or hide)


### Requirement 21: Menu System

**User Story:** As a user, I want a consistent menu system for accessing application features, so that I can discover and use functionality efficiently.

#### Acceptance Criteria

1. WHEN menu roots are displayed THEN the system SHALL use 14px font size with regular (400) weight
2. WHEN menu items are displayed THEN the system SHALL use 36px height with 4px vertical and 16px horizontal padding
3. WHEN menu items have keyboard shortcuts THEN the system SHALL display the shortcut text aligned to the right with reduced opacity
4. WHEN menu items are hovered THEN the system SHALL display the accent color background with reduced opacity
5. WHEN menu folders (submenus) are displayed THEN the system SHALL show a right-pointing arrow icon (pan-end-symbolic)
6. WHEN menu dividers are displayed THEN the system SHALL use a 1px horizontal line with neutral color
7. WHEN checkbox menu items are displayed THEN the system SHALL show a checkmark icon (object-select-symbolic) when checked

### Requirement 22: Header Bar with Window Controls

**User Story:** As a user, I want a header bar with window controls, so that I can manage the application window.

#### Acceptance Criteria

1. WHEN the header bar is displayed THEN the system SHALL use 32px base height plus padding (40-48px total)
2. WHEN window control buttons are displayed THEN the system SHALL use symbolic icons (window-minimize-symbolic, window-maximize-symbolic, window-close-symbolic)
3. WHEN the window is maximized THEN the system SHALL display window-restore-symbolic instead of window-maximize-symbolic
4. WHEN the header bar is focused THEN the system SHALL use full opacity for title and controls
5. WHEN the header bar is unfocused THEN the system SHALL reduce opacity to 75-80% for title and controls
6. WHEN window control buttons are displayed THEN the system SHALL use 16px icon size with 8px padding
7. WHEN the header bar is dragged THEN the system SHALL emit a drag-requested callback for window movement

### Requirement 23: Segmented Button for Tabs

**User Story:** As a user, I want tab bars to clearly show available sections, so that I can switch between different views.

#### Acceptance Criteria

1. WHEN segmented buttons are displayed THEN the system SHALL use 32px or 44px height depending on context
2. WHEN segmented button items are displayed THEN the system SHALL show dividers between items
3. WHEN a segmented button item is selected THEN the system SHALL display the accent color background with reduced opacity
4. WHEN segmented button items have close buttons THEN the system SHALL display a small close icon (8px) on hover
5. WHEN segmented buttons are used as tabs THEN the system SHALL use 76px minimum width and 250px maximum width per item

### Requirement 24: Popover and Context Menu

**User Story:** As a user, I want context menus and popovers to appear near relevant content, so that I can access contextual actions easily.

#### Acceptance Criteria

1. WHEN a popover is displayed THEN the system SHALL position it relative to the trigger element (center, bottom, or custom point)
2. WHEN a modal popover is displayed THEN the system SHALL intercept all user inputs outside the popover
3. WHEN a non-modal popover is displayed THEN the system SHALL close when clicking outside the popover bounds
4. WHEN a popover is displayed THEN the system SHALL ensure it stays within viewport bounds
5. WHEN a popover position would exceed viewport THEN the system SHALL adjust position to fit

### Requirement 25: Typography Presets

**User Story:** As a developer, I want predefined typography presets, so that I can maintain consistent text styling throughout the application.

#### Acceptance Criteria

1. WHEN title1 text is displayed THEN the system SHALL use 35px font size with 600 (semibold) weight and 52px line height
2. WHEN title2 text is displayed THEN the system SHALL use 29px font size with 600 (semibold) weight and 43px line height
3. WHEN title3 text is displayed THEN the system SHALL use 24px font size with 700 (bold) weight and 36px line height
4. WHEN title4 text is displayed THEN the system SHALL use 20px font size with 700 (bold) weight and 30px line height
5. WHEN heading text is displayed THEN the system SHALL use 14px font size with 700 (bold) weight and 21px line height
6. WHEN body text is displayed THEN the system SHALL use 14px font size with 400 (regular) weight and 21px line height
7. WHEN caption text is displayed THEN the system SHALL use 12px font size with 400 (regular) weight and 17px line height
8. WHEN caption-heading text is displayed THEN the system SHALL use 12px font size with 600 (semibold) weight and 17px line height

### Requirement 26: Focus Management

**User Story:** As a user, I want clear focus indicators, so that I can navigate the application with keyboard efficiently.

#### Acceptance Criteria

1. WHEN an interactive element receives focus THEN the system SHALL display a 2px outline in the accent color
2. WHEN focus moves between elements THEN the system SHALL update the focus indicator smoothly
3. WHEN a button is focused and Enter is pressed THEN the system SHALL trigger the button's action
4. WHEN a dialog is opened THEN the system SHALL focus the first interactive element
5. WHEN Escape is pressed in a dialog THEN the system SHALL close the dialog and return focus to the trigger element

### Requirement 27: Selection Indicators

**User Story:** As a user, I want clear visual indicators for selected items, so that I can see what is currently active.

#### Acceptance Criteria

1. WHEN an item is selected THEN the system SHALL display a checkmark icon (object-select-symbolic) with accent color
2. WHEN image buttons are selected THEN the system SHALL display a selection indicator in the bottom-left corner
3. WHEN navigation items are selected THEN the system SHALL use accent color background with 15% opacity
4. WHEN list items are selected THEN the system SHALL use accent color background with 10-15% opacity
5. WHEN tab items are selected THEN the system SHALL use accent color background with appropriate opacity

### Requirement 28: Responsive Menu Bar

**User Story:** As a user, I want the menu bar to adapt to narrow windows, so that I can access all features regardless of window size.

#### Acceptance Criteria

1. WHEN the window width is insufficient for all menu items THEN the system SHALL collapse the menu bar into a hamburger menu
2. WHEN the collapsed menu is displayed THEN the system SHALL show an "open-menu-symbolic" icon button
3. WHEN the collapsed menu is clicked THEN the system SHALL display all menu items in a dropdown
4. WHEN the window is resized to accommodate all items THEN the system SHALL expand back to full menu bar
5. WHEN the menu bar tracks size THEN the system SHALL emit size-changed events for responsive behavior
6. WHEN the collapsed menu dropdown is displayed THEN the system SHALL use 150px item width
7. WHEN menu items are in collapsed state THEN the system SHALL maintain all functionality including submenus and shortcuts

### Requirement 29: Button Size Presets

**User Story:** As a developer, I want predefined button size presets, so that I can maintain consistent button sizing throughout the application.

#### Acceptance Criteria

1. WHEN extra small buttons are used THEN the system SHALL use 14px font, 16px icon, 20px line height, and space-xxs padding
2. WHEN small buttons are used THEN the system SHALL use 14px font, 20px icon, 20px line height, and space-xs padding
3. WHEN medium buttons are used THEN the system SHALL use 24px font, 32px icon, 32px line height, and space-xs padding
4. WHEN large buttons are used THEN the system SHALL use 28px font, 40px icon, 36px line height, and space-xs padding
5. WHEN extra large buttons are used THEN the system SHALL use 32px font (light weight), 56px icon, 44px line height, and space-xs padding

### Requirement 30: Icon Button Variants

**User Story:** As a user, I want icon buttons for compact actions, so that I can access functionality without text labels.

#### Acceptance Criteria

1. WHEN icon buttons are displayed THEN the system SHALL use symbolic icons at 16px for standard size
2. WHEN icon buttons are displayed THEN the system SHALL use non-symbolic icons at 24px for standard size
3. WHEN icon buttons have labels THEN the system SHALL display text below icon in vertical layout
4. WHEN icon buttons are in vertical mode THEN the system SHALL center-align icon and text
5. WHEN icon buttons are in horizontal mode THEN the system SHALL place icon before text with space-xxxs spacing
6. WHEN icon buttons are selected THEN the system SHALL show selected state with accent color
7. WHEN icon buttons have tooltips THEN the system SHALL display them on hover after 500ms delay

### Requirement 31: Image Button Component

**User Story:** As a user, I want image buttons for visual selections, so that I can choose from image-based options.

#### Acceptance Criteria

1. WHEN image buttons are displayed THEN the system SHALL use 9px border radius
2. WHEN image buttons are selected THEN the system SHALL show selection indicator at bottom-left corner
3. WHEN image buttons have remove action THEN the system SHALL show close icon (×) at top-right on hover
4. WHEN image buttons are hovered THEN the system SHALL show subtle overlay or border highlight
5. WHEN image button close icon is clicked THEN the system SHALL emit remove event
6. WHEN image buttons are in a grid THEN the system SHALL maintain consistent sizing and spacing

### Requirement 32: Link Button Component

**User Story:** As a user, I want link-style buttons for navigation actions, so that I can distinguish navigation from primary actions.

#### Acceptance Criteria

1. WHEN link buttons are displayed THEN the system SHALL use accent color text with no background
2. WHEN link buttons are hovered THEN the system SHALL underline the text
3. WHEN link buttons have trailing icons THEN the system SHALL show external-link icon on the right
4. WHEN link buttons are displayed THEN the system SHALL use 14px font size with 400 (regular) weight
5. WHEN link buttons are focused THEN the system SHALL show 2px accent outline
6. WHEN link buttons are disabled THEN the system SHALL reduce opacity to 40%

### Requirement 33: Text Button Variants

**User Story:** As a user, I want different button styles for different action priorities, so that I can understand action hierarchy.

#### Acceptance Criteria

1. WHEN standard buttons are displayed THEN the system SHALL use accent background with white text
2. WHEN suggested buttons are displayed THEN the system SHALL use accent background with enhanced prominence
3. WHEN destructive buttons are displayed THEN the system SHALL use red/error color for background
4. WHEN text-only buttons are displayed THEN the system SHALL use transparent background with accent text
5. WHEN buttons have leading icons THEN the system SHALL place icon before text with space-xxxs spacing
6. WHEN buttons have trailing icons THEN the system SHALL place icon after text with space-xxxs spacing
7. WHEN button text is displayed THEN the system SHALL use 14px font size with 500 (medium) weight

### Requirement 34: Button State Management

**User Story:** As a user, I want clear visual feedback for button states, so that I understand button interactivity.

#### Acceptance Criteria

1. WHEN buttons are in normal state THEN the system SHALL display default styling
2. WHEN buttons are hovered THEN the system SHALL darken background by 10% or show shadow
3. WHEN buttons are pressed THEN the system SHALL darken background by 15% and reduce shadow
4. WHEN buttons are focused THEN the system SHALL show 2px accent color outline
5. WHEN buttons are disabled THEN the system SHALL reduce opacity to 40% and show not-allowed cursor
6. WHEN buttons are in loading state THEN the system SHALL show spinner and disable interaction
7. WHEN button states transition THEN the system SHALL animate over 150ms with ease-out

### Requirement 35: Container Component Styling

**User Story:** As a user, I want consistent container styling, so that content grouping is clear and organized.

#### Acceptance Criteria

1. WHEN containers are displayed THEN the system SHALL use appropriate background for current layer
2. WHEN containers are on background layer THEN the system SHALL use background.base color
3. WHEN containers are on primary layer THEN the system SHALL use primary.base color
4. WHEN containers are on secondary layer THEN the system SHALL use secondary.base color
5. WHEN containers have borders THEN the system SHALL use 1px width with neutral color
6. WHEN containers have padding THEN the system SHALL use 16-24px based on content density
7. WHEN containers are elevated THEN the system SHALL use subtle shadow (10-20% opacity)

### Requirement 36: Divider Component

**User Story:** As a user, I want dividers to separate content sections, so that information is organized clearly.

#### Acceptance Criteria

1. WHEN horizontal dividers are displayed THEN the system SHALL use 1px height with neutral color
2. WHEN vertical dividers are displayed THEN the system SHALL use 1px width with neutral color
3. WHEN dividers are in light mode THEN the system SHALL use divider color with appropriate opacity
4. WHEN dividers are in dark mode THEN the system SHALL adjust color for dark backgrounds
5. WHEN dividers have labels THEN the system SHALL display text centered with spacing on both sides
6. WHEN dividers are used in lists THEN the system SHALL span full width with appropriate margins

### Requirement 37: Spacing System Implementation

**User Story:** As a developer, I want a comprehensive spacing system, so that I can maintain consistent spacing throughout the application.

#### Acceptance Criteria

1. WHEN space-xxxs is used THEN the system SHALL apply 4px spacing
2. WHEN space-xxs is used THEN the system SHALL apply 8px spacing
3. WHEN space-xs is used THEN the system SHALL apply 12px spacing
4. WHEN space-s is used THEN the system SHALL apply 16px spacing
5. WHEN space-m is used THEN the system SHALL apply 24px spacing
6. WHEN space-l is used THEN the system SHALL apply 32px spacing
7. WHEN space-xl is used THEN the system SHALL apply 48px spacing
8. WHEN space-xxl is used THEN the system SHALL apply 64px spacing

### Requirement 38: Corner Radii System

**User Story:** As a developer, I want a consistent corner radius system, so that all rounded elements follow the same scale.

#### Acceptance Criteria

1. WHEN radius-0 is used THEN the system SHALL apply 0px (sharp corners)
2. WHEN radius-xs is used THEN the system SHALL apply 4px border radius
3. WHEN radius-s is used THEN the system SHALL apply 8px border radius
4. WHEN radius-m is used THEN the system SHALL apply 12px border radius
5. WHEN radius-l is used THEN the system SHALL apply 16px border radius
6. WHEN radius-xl is used THEN the system SHALL apply 20px border radius
7. WHEN radius-xxl is used THEN the system SHALL apply 24px border radius
8. WHEN radius-full is used THEN the system SHALL apply 9999px (fully rounded/circular)

### Requirement 39: Color Semantic Naming

**User Story:** As a developer, I want semantic color names, so that I can use colors based on their purpose rather than their appearance.

#### Acceptance Criteria

1. WHEN accent colors are referenced THEN the system SHALL provide accent-primary, accent-secondary, accent-tertiary
2. WHEN text colors are referenced THEN the system SHALL provide text-primary, text-secondary, text-disabled
3. WHEN background colors are referenced THEN the system SHALL provide background-base, background-surface, background-overlay
4. WHEN state colors are referenced THEN the system SHALL provide success, warning, error, info
5. WHEN component colors are referenced THEN the system SHALL provide component-base, component-hover, component-pressed
6. WHEN border colors are referenced THEN the system SHALL provide border-neutral, border-accent, border-error
7. WHEN all colors are defined THEN the system SHALL provide both light and dark mode variants

### Requirement 40: Settings Panel Pattern

**User Story:** As a user, I want consistent settings panels, so that I can configure application preferences easily.

#### Acceptance Criteria

1. WHEN settings sections are displayed THEN the system SHALL show section title with 16px semibold font
2. WHEN settings sections have descriptions THEN the system SHALL display them below title with 14px regular font
3. WHEN settings rows are displayed THEN the system SHALL use horizontal layout with label on left and control on right
4. WHEN settings row labels are displayed THEN the system SHALL use 14px font with optional description below
5. WHEN settings rows have descriptions THEN the system SHALL use 12px secondary text color
6. WHEN settings sections are displayed THEN the system SHALL use space-m spacing between sections
7. WHEN settings rows are displayed THEN the system SHALL use space-s spacing between rows
8. WHEN settings controls are displayed THEN the system SHALL align them to the right side of the row

### Requirement 41: Navigation Bar Component

**User Story:** As a user, I want a navigation sidebar for switching between views, so that I can easily navigate the application.

#### Acceptance Criteria

1. WHEN the navigation bar is displayed THEN the system SHALL use 32px button height with appropriate padding
2. WHEN navigation items are displayed THEN the system SHALL show icons with optional text labels
3. WHEN a navigation item is selected THEN the system SHALL highlight it with the accent color
4. WHEN the navigation bar contains many items THEN the system SHALL provide scrolling functionality
5. WHEN navigation items have context menus THEN the system SHALL display them on right-click
6. WHEN navigation items support drag and drop THEN the system SHALL provide visual feedback during drag operations
7. WHEN the navigation bar is collapsed THEN the system SHALL show only icons without text labels

### Requirement 42: Menu Bar and Dropdown Menus

**User Story:** As a user, I want hierarchical menu systems, so that I can access application commands efficiently.

#### Acceptance Criteria

1. WHEN menu roots are displayed THEN the system SHALL use 4-12px padding with appropriate spacing
2. WHEN menu items are hovered THEN the system SHALL highlight them with subtle background color
3. WHEN menu items have keyboard shortcuts THEN the system SHALL display them aligned to the right
4. WHEN menu items are disabled THEN the system SHALL reduce opacity to 40%
5. WHEN submenus exist THEN the system SHALL show a right-pointing arrow indicator
6. WHEN menus are opened THEN the system SHALL position them adaptively to stay within viewport bounds
7. WHEN menu items have icons THEN the system SHALL display them at 14-16px size before the label
8. WHEN menu items are checkboxes THEN the system SHALL show a checkmark icon when selected

### Requirement 43: Segmented Button Controls

**User Story:** As a user, I want segmented button controls for related options, so that I can make selections from grouped choices.

#### Acceptance Criteria

1. WHEN segmented buttons are displayed THEN the system SHALL join them visually with shared borders
2. WHEN a segment is selected THEN the system SHALL highlight it with accent color background
3. WHEN segments are in horizontal layout THEN the system SHALL use 32px height with center alignment
4. WHEN segments are in vertical layout THEN the system SHALL stack them with consistent spacing
5. WHEN segments have icons THEN the system SHALL display them at 16-20px size
6. WHEN segments support single selection THEN the system SHALL ensure only one is active at a time
7. WHEN segments support multiple selection THEN the system SHALL allow multiple active states

### Requirement 44: Tab Bar Component

**User Story:** As a user, I want tab bars for switching between content views, so that I can organize multiple views efficiently.

#### Acceptance Criteria

1. WHEN tabs are displayed THEN the system SHALL use 44px height with 76-250px width range
2. WHEN a tab is active THEN the system SHALL show an accent color indicator below it
3. WHEN tabs have close buttons THEN the system SHALL display them on hover or when active
4. WHEN tabs support drag and drop THEN the system SHALL allow reordering via drag operations
5. WHEN tabs overflow the available space THEN the system SHALL provide scrolling or overflow menu
6. WHEN tabs have icons THEN the system SHALL display them before the text label
7. WHEN tabs are closable THEN the system SHALL show a close button (×) on the right side

### Requirement 45: Header Bar Component

**User Story:** As a user, I want a consistent header bar for windows, so that I have familiar window controls and title display.

#### Acceptance Criteria

1. WHEN the header bar is displayed THEN the system SHALL use 32-40px height based on density
2. WHEN window controls are shown THEN the system SHALL display minimize, maximize, and close buttons
3. WHEN the window is maximized THEN the system SHALL adjust padding and show restore icon
4. WHEN the window is focused THEN the system SHALL use full opacity for controls
5. WHEN the window is unfocused THEN the system SHALL reduce control opacity to 75%
6. WHEN the header bar is dragged THEN the system SHALL move the window
7. WHEN the header bar is double-clicked THEN the system SHALL toggle maximize state
8. WHEN custom content is added THEN the system SHALL support start, center, and end regions

### Requirement 46: Popover Component

**User Story:** As a user, I want popover overlays for contextual content, so that I can see additional information without leaving my current context.

#### Acceptance Criteria

1. WHEN a popover is opened THEN the system SHALL position it relative to the trigger element
2. WHEN a popover is modal THEN the system SHALL block interaction with underlying content
3. WHEN a popover is non-modal THEN the system SHALL close it when clicking outside
4. WHEN a popover exceeds viewport bounds THEN the system SHALL reposition it to stay visible
5. WHEN a popover has a backdrop THEN the system SHALL fade it in over 200ms
6. WHEN a popover is closed THEN the system SHALL emit a close event
7. WHEN multiple popovers are open THEN the system SHALL manage their z-index hierarchy

### Requirement 47: Warning and Alert Components

**User Story:** As a user, I want clear warning and alert messages, so that I understand important information and potential issues.

#### Acceptance Criteria

1. WHEN warnings are displayed THEN the system SHALL use warning color background (#F39C12)
2. WHEN alerts have close buttons THEN the system SHALL display them on the right side
3. WHEN warning text is displayed THEN the system SHALL ensure sufficient contrast for readability
4. WHEN warnings contain icons THEN the system SHALL use 16px size with appropriate spacing
5. WHEN warnings are dismissible THEN the system SHALL provide a close button
6. WHEN warnings are persistent THEN the system SHALL omit the close button
7. WHEN multiple warnings exist THEN the system SHALL stack them with appropriate spacing

### Requirement 48: Card Component Styling

**User Story:** As a user, I want consistent card styling for grouped content, so that related information is visually organized.

#### Acceptance Criteria

1. WHEN cards are displayed THEN the system SHALL use appropriate layer background colors
2. WHEN cards are on background layer THEN the system SHALL use component hover color
3. WHEN cards are on primary layer THEN the system SHALL use primary component colors
4. WHEN cards are on secondary layer THEN the system SHALL use secondary component colors
5. WHEN cards have elevation THEN the system SHALL use subtle shadows (10-20% opacity)
6. WHEN cards have borders THEN the system SHALL use 1px width with neutral colors
7. WHEN cards are interactive THEN the system SHALL show hover state with background change

### Requirement 49: Scrollable Container Styling

**User Story:** As a user, I want minimal scrollbar styling, so that scrollable content doesn't feel cluttered.

#### Acceptance Criteria

1. WHEN scrollbars are displayed THEN the system SHALL use minimal width (6-8px)
2. WHEN scrollbars are inactive THEN the system SHALL reduce their opacity
3. WHEN scrollbars are hovered THEN the system SHALL increase their opacity
4. WHEN scrolling occurs THEN the system SHALL show scrollbar temporarily
5. WHEN scrolling stops THEN the system SHALL fade out scrollbar after 1 second
6. WHEN content is scrollable THEN the system SHALL indicate scroll availability subtly
7. WHEN scrollbars are styled THEN the system SHALL match the current theme colors

### Requirement 50: Responsive Container System

**User Story:** As a developer, I want responsive containers that adapt to size changes, so that layouts work across different window sizes.

#### Acceptance Criteria

1. WHEN container size changes THEN the system SHALL emit size change events
2. WHEN containers have minimum width THEN the system SHALL enforce it (320px default)
3. WHEN containers have maximum width THEN the system SHALL enforce it (1200px default)
4. WHEN containers collapse THEN the system SHALL trigger layout recalculation
5. WHEN navigation should collapse THEN the system SHALL hide text labels and show only icons
6. WHEN size thresholds are crossed THEN the system SHALL notify parent components
7. WHEN responsive behavior is configured THEN the system SHALL respect custom breakpoints

### Requirement 51: Drag and Drop Visual Feedback

**User Story:** As a user, I want clear visual feedback during drag and drop operations, so that I understand where items can be dropped.

#### Acceptance Criteria

1. WHEN dragging starts THEN the system SHALL show a drag preview with reduced opacity
2. WHEN dragging over valid targets THEN the system SHALL highlight them with accent color
3. WHEN dragging over invalid targets THEN the system SHALL show no highlight
4. WHEN drop is accepted THEN the system SHALL provide visual confirmation
5. WHEN drop is rejected THEN the system SHALL animate the item back to origin
6. WHEN dragging between containers THEN the system SHALL show insertion indicators
7. WHEN drag operation ends THEN the system SHALL restore normal visual state

### Requirement 52: Context Menu Behavior

**User Story:** As a user, I want context menus that appear on right-click, so that I can access contextual actions easily.

#### Acceptance Criteria

1. WHEN right-clicking an element THEN the system SHALL display its context menu
2. WHEN context menu is displayed THEN the system SHALL position it at cursor location
3. WHEN context menu exceeds viewport THEN the system SHALL reposition it to stay visible
4. WHEN clicking outside context menu THEN the system SHALL close it
5. WHEN selecting a context menu item THEN the system SHALL execute the action and close the menu
6. WHEN context menu has submenus THEN the system SHALL open them on hover
7. WHEN context menu items are disabled THEN the system SHALL show them with reduced opacity

### Requirement 53: Keyboard Navigation Enhancement

**User Story:** As a user, I want comprehensive keyboard navigation, so that I can use the application efficiently without a mouse.

#### Acceptance Criteria

1. WHEN Tab is pressed THEN the system SHALL move focus to next focusable element
2. WHEN Shift+Tab is pressed THEN the system SHALL move focus to previous focusable element
3. WHEN arrow keys are pressed in menus THEN the system SHALL navigate menu items
4. WHEN Enter is pressed on focused element THEN the system SHALL activate it
5. WHEN Escape is pressed THEN the system SHALL close current overlay or cancel operation
6. WHEN Space is pressed on checkbox THEN the system SHALL toggle its state
7. WHEN keyboard shortcuts are pressed THEN the system SHALL execute corresponding actions

### Requirement 54: Focus Indicator Styling

**User Story:** As a user, I want clear focus indicators, so that I know which element has keyboard focus.

#### Acceptance Criteria

1. WHEN an element receives focus THEN the system SHALL display a 2px accent color outline
2. WHEN focus moves between elements THEN the system SHALL animate the transition smoothly
3. WHEN focus is on buttons THEN the system SHALL show outline with 2px offset
4. WHEN focus is on inputs THEN the system SHALL show outline around the input border
5. WHEN focus is on custom elements THEN the system SHALL provide appropriate focus styling
6. WHEN focus indicators are displayed THEN the system SHALL ensure they are visible against all backgrounds
7. WHEN focus is programmatically set THEN the system SHALL show the same visual indicator

### Requirement 55: About Dialog Pattern

**User Story:** As a user, I want a standardized about dialog, so that I can learn about the application and its contributors.

#### Acceptance Criteria

1. WHEN about dialog is opened THEN the system SHALL display app name, version, and icon
2. WHEN about dialog shows contributors THEN the system SHALL group them by role (developers, designers, etc.)
3. WHEN about dialog has links THEN the system SHALL make them clickable with proper styling
4. WHEN about dialog shows license THEN the system SHALL display license name and link
5. WHEN about dialog shows copyright THEN the system SHALL display it at the bottom
6. WHEN about dialog has comments THEN the system SHALL display them as body text
7. WHEN about dialog is displayed THEN the system SHALL center content with appropriate spa


## Component Inventory and Detailed Requirements

### Requirement 56: Text Input Components

**User Story:** As a user, I want consistent text input fields, so that I can enter data efficiently across the application.

#### Acceptance Criteria

1. WHEN text inputs are displayed THEN the system SHALL use 32px height for standard inputs
2. WHEN text inputs have labels THEN the system SHALL display them above with 14px semibold font
3. WHEN text inputs receive focus THEN the system SHALL show 2px accent color border
4. WHEN text inputs contain errors THEN the system SHALL show red border and error message below
5. WHEN text inputs are disabled THEN the system SHALL reduce opacity to 40% and prevent interaction
6. WHEN text inputs have placeholders THEN the system SHALL use secondary text color
7. WHEN text inputs support clear button THEN the system SHALL show it on hover when text is present
8. WHEN text inputs are multiline THEN the system SHALL support textarea with minimum 3 rows
9. WHEN text inputs have character limits THEN the system SHALL display counter (e.g., "45/100")
10. WHEN text inputs support search THEN the system SHALL include search icon on the left

### Requirement 57: Checkbox and Radio Components

**User Story:** As a user, I want clear checkbox and radio button controls, so that I can make selections easily.

#### Acceptance Criteria

1. WHEN checkboxes are displayed THEN the system SHALL use 20px × 20px size with 4px border radius
2. WHEN checkboxes are checked THEN the system SHALL show checkmark icon with accent color background
3. WHEN checkboxes are indeterminate THEN the system SHALL show minus icon
4. WHEN checkboxes have labels THEN the system SHALL place them to the right with 8px spacing
5. WHEN radio buttons are displayed THEN the system SHALL use 20px circular shape
6. WHEN radio buttons are selected THEN the system SHALL show filled inner circle with accent color
7. WHEN checkboxes/radios are disabled THEN the system SHALL reduce opacity to 40%
8. WHEN checkboxes/radios receive focus THEN the system SHALL show 2px accent outline
9. WHEN checkbox groups exist THEN the system SHALL support parent checkbox for select-all
10. WHEN radio groups exist THEN the system SHALL ensure only one selection at a time

### Requirement 58: Toggle Switch Components

**User Story:** As a user, I want toggle switches for binary options, so that I can quickly enable/disable features.

#### Acceptance Criteria

1. WHEN toggles are displayed THEN the system SHALL use 24px height with rounded pill shape
2. WHEN toggles are off THEN the system SHALL use neutral background color
3. WHEN toggles are on THEN the system SHALL use accent color background
4. WHEN toggles transition THEN the system SHALL animate the knob over 150ms
5. WHEN toggles have labels THEN the system SHALL place them to the right with 8px spacing
6. WHEN toggles are disabled THEN the system SHALL reduce opacity to 40%
7. WHEN toggles receive focus THEN the system SHALL show 2px accent outline
8. WHEN toggles are in loading state THEN the system SHALL show spinner on the knob
9. WHEN toggles have descriptions THEN the system SHALL display them below in smaller text
10. WHEN toggles are in forms THEN the system SHALL align them consistently with other inputs

### Requirement 59: Slider Components

**User Story:** As a user, I want slider controls for range selection, so that I can adjust values visually.

#### Acceptance Criteria

1. WHEN sliders are displayed THEN the system SHALL use 4px track height
2. WHEN sliders show progress THEN the system SHALL fill track with accent color up to thumb position
3. WHEN slider thumbs are displayed THEN the system SHALL use 16px circular shape
4. WHEN slider thumbs are dragged THEN the system SHALL show larger size (20px) and shadow
5. WHEN sliders have min/max labels THEN the system SHALL display them at track ends
6. WHEN sliders have value labels THEN the system SHALL show current value above thumb
7. WHEN sliders have tick marks THEN the system SHALL display them below track at intervals
8. WHEN sliders are disabled THEN the system SHALL reduce opacity to 40%
9. WHEN sliders support range THEN the system SHALL provide two thumbs for min/max selection
10. WHEN sliders are vertical THEN the system SHALL rotate layout 90 degrees

### Requirement 60: Dropdown/Select Components

**User Story:** As a user, I want dropdown menus for selecting from lists, so that I can choose options efficiently.

#### Acceptance Criteria

1. WHEN dropdowns are displayed THEN the system SHALL use 32px height with down arrow icon
2. WHEN dropdowns are opened THEN the system SHALL show options list with max 8 visible items
3. WHEN dropdown options are hovered THEN the system SHALL highlight with subtle background
4. WHEN dropdown options are selected THEN the system SHALL show checkmark icon
5. WHEN dropdowns support search THEN the system SHALL include search input at top of list
6. WHEN dropdowns support multi-select THEN the system SHALL show checkboxes for each option
7. WHEN dropdowns have groups THEN the system SHALL show group headers with dividers
8. WHEN dropdowns are disabled THEN the system SHALL reduce opacity to 40%
9. WHEN dropdown lists exceed viewport THEN the system SHALL provide scrolling
10. WHEN dropdowns have icons THEN the system SHALL display them before option text

### Requirement 61: Spinner/Loading Components

**User Story:** As a user, I want clear loading indicators, so that I know when the system is processing.

#### Acceptance Criteria

1. WHEN spinners are displayed THEN the system SHALL use circular rotating animation
2. WHEN spinners are small THEN the system SHALL use 16px size for inline contexts
3. WHEN spinners are medium THEN the system SHALL use 32px size for buttons
4. WHEN spinners are large THEN the system SHALL use 64px size for page loading
5. WHEN spinners use accent color THEN the system SHALL apply it to the rotating arc
6. WHEN progress is determinate THEN the system SHALL show circular progress with percentage
7. WHEN progress bars are displayed THEN the system SHALL use 4px height with rounded ends
8. WHEN progress bars show value THEN the system SHALL fill with accent color proportionally
9. WHEN loading overlays are shown THEN the system SHALL dim background and center spinner
10. WHEN skeleton screens are used THEN the system SHALL show pulsing placeholder shapes

### Requirement 62: Badge and Chip Components

**User Story:** As a user, I want badges and chips for labels and tags, so that I can see categorized information clearly.

#### Acceptance Criteria

1. WHEN badges are displayed THEN the system SHALL use 20px height with 10px border radius
2. WHEN badges show counts THEN the system SHALL use accent color background
3. WHEN badges are on buttons THEN the system SHALL position them at top-right corner
4. WHEN badges exceed 99 THEN the system SHALL display "99+"
5. WHEN chips are displayed THEN the system SHALL use 28px height with full border radius
6. WHEN chips are removable THEN the system SHALL show close icon (×) on the right
7. WHEN chips have avatars THEN the system SHALL display them on the left at 24px size
8. WHEN chips are clickable THEN the system SHALL show hover state with background change
9. WHEN chips are in input fields THEN the system SHALL allow inline display with wrapping
10. WHEN status badges are shown THEN the system SHALL use semantic colors (success, warning, error)

### Requirement 63: Avatar Components

**User Story:** As a user, I want avatar components for user representation, so that I can identify users visually.

#### Acceptance Criteria

1. WHEN avatars are displayed THEN the system SHALL use circular shape by default
2. WHEN avatars are extra small THEN the system SHALL use 24px size
3. WHEN avatars are small THEN the system SHALL use 32px size
4. WHEN avatars are medium THEN the system SHALL use 40px size
5. WHEN avatars are large THEN the system SHALL use 56px size
6. WHEN avatars have no image THEN the system SHALL show initials with colored background
7. WHEN avatars show status THEN the system SHALL display indicator dot at bottom-right
8. WHEN avatar groups are displayed THEN the system SHALL overlap them with -8px margin
9. WHEN avatars are clickable THEN the system SHALL show hover state with border
10. WHEN avatars support square shape THEN the system SHALL use 8px border radius

### Requirement 64: Tooltip Components

**User Story:** As a user, I want tooltips for additional information, so that I can understand UI elements without cluttering the interface.

#### Acceptance Criteria

1. WHEN tooltips are triggered THEN the system SHALL show them after 500ms hover delay
2. WHEN tooltips are displayed THEN the system SHALL use dark background with white text
3. WHEN tooltips have arrows THEN the system SHALL point them toward the trigger element
4. WHEN tooltips exceed viewport THEN the system SHALL reposition to stay visible
5. WHEN tooltips contain text THEN the system SHALL use 12px font size
6. WHEN tooltips are dismissed THEN the system SHALL fade out over 150ms
7. WHEN tooltips support rich content THEN the system SHALL allow custom formatting
8. WHEN tooltips are on touch devices THEN the system SHALL show on tap and dismiss on second tap
9. WHEN tooltips have max width THEN the system SHALL wrap text at 300px
10. WHEN multiple tooltips exist THEN the system SHALL show only one at a time

### Requirement 65: Dialog/Modal Components

**User Story:** As a user, I want modal dialogs for important interactions, so that I can focus on specific tasks.

#### Acceptance Criteria

1. WHEN dialogs are opened THEN the system SHALL show backdrop with 50% opacity overlay
2. WHEN dialogs are displayed THEN the system SHALL center them in viewport
3. WHEN dialogs have titles THEN the system SHALL use 20px bold font in header
4. WHEN dialogs have close buttons THEN the system SHALL place them at top-right corner
5. WHEN dialogs have actions THEN the system SHALL place them at bottom-right (primary rightmost)
6. WHEN dialogs are modal THEN the system SHALL trap focus within dialog
7. WHEN dialogs are opened THEN the system SHALL animate scale from 0.95 to 1.0 over 200ms
8. WHEN Escape is pressed THEN the system SHALL close the dialog
9. WHEN dialogs have scrollable content THEN the system SHALL fix header and footer
10. WHEN dialogs support sizes THEN the system SHALL provide small (400px), medium (600px), large (800px)

### Requirement 66: Drawer/Sidebar Components

**User Story:** As a user, I want slide-out drawers for additional content, so that I can access secondary information without leaving my context.

#### Acceptance Criteria

1. WHEN drawers are opened THEN the system SHALL slide in from specified edge (left/right/top/bottom)
2. WHEN drawers animate THEN the system SHALL transition over 300ms with ease-out
3. WHEN drawers have backdrop THEN the system SHALL show it with 50% opacity
4. WHEN drawers are modal THEN the system SHALL block interaction with main content
5. WHEN drawers are non-modal THEN the system SHALL push main content aside
6. WHEN drawers have headers THEN the system SHALL fix them at top with close button
7. WHEN drawers have footers THEN the system SHALL fix them at bottom
8. WHEN drawers support sizes THEN the system SHALL provide 256px (narrow), 400px (standard), 600px (wide)
9. WHEN drawers are closed THEN the system SHALL slide out and remove from DOM
10. WHEN drawers are on mobile THEN the system SHALL use full width

### Requirement 67: Accordion/Collapsible Components

**User Story:** As a user, I want accordion components for expandable content, so that I can manage information density.

#### Acceptance Criteria

1. WHEN accordions are displayed THEN the system SHALL show headers with expand/collapse icons
2. WHEN accordion items are expanded THEN the system SHALL rotate icon 90 degrees
3. WHEN accordion content expands THEN the system SHALL animate height over 200ms
4. WHEN accordion headers are clicked THEN the system SHALL toggle expansion state
5. WHEN accordions support single expansion THEN the system SHALL collapse others when one opens
6. WHEN accordions support multiple expansion THEN the system SHALL allow multiple open items
7. WHEN accordion items have borders THEN the system SHALL use 1px neutral color
8. WHEN accordion headers are hovered THEN the system SHALL show subtle background highlight
9. WHEN accordion items are disabled THEN the system SHALL reduce opacity and prevent interaction
10. WHEN accordions are nested THEN the system SHALL indent child items by 16px

### Requirement 68: Breadcrumb Components

**User Story:** As a user, I want breadcrumb navigation, so that I can understand my location and navigate back easily.

#### Acceptance Criteria

1. WHEN breadcrumbs are displayed THEN the system SHALL separate items with "/" or ">" separator
2. WHEN breadcrumb items are clickable THEN the system SHALL show hover state with underline
3. WHEN breadcrumbs exceed available width THEN the system SHALL collapse middle items to "..."
4. WHEN breadcrumb separators are shown THEN the system SHALL use secondary text color
5. WHEN current page is shown THEN the system SHALL display it without link styling
6. WHEN breadcrumbs have icons THEN the system SHALL display them before text at 16px
7. WHEN breadcrumbs are on mobile THEN the system SHALL show only last 2 items
8. WHEN breadcrumb items are long THEN the system SHALL truncate with ellipsis at 150px
9. WHEN breadcrumbs have dropdown THEN the system SHALL show collapsed items in menu
10. WHEN breadcrumbs use semantic HTML THEN the system SHALL use nav with aria-label

### Requirement 69: Pagination Components

**User Story:** As a user, I want pagination controls, so that I can navigate through large datasets.

#### Acceptance Criteria

1. WHEN pagination is displayed THEN the system SHALL show page numbers with prev/next buttons
2. WHEN current page is shown THEN the system SHALL highlight it with accent color
3. WHEN pagination has many pages THEN the system SHALL show first, last, and ellipsis for middle
4. WHEN prev button is on first page THEN the system SHALL disable it
5. WHEN next button is on last page THEN the system SHALL disable it
6. WHEN page numbers are clicked THEN the system SHALL navigate to that page
7. WHEN pagination shows page size THEN the system SHALL provide dropdown for items per page
8. WHEN pagination shows total THEN the system SHALL display "Showing 1-10 of 100"
9. WHEN pagination is on mobile THEN the system SHALL show compact version with only prev/next
10. WHEN pagination supports jump THEN the system SHALL provide input for direct page entry

### Requirement 70: Tree View Components

**User Story:** As a user, I want tree view components for hierarchical data, so that I can navigate nested structures.

#### Acceptance Criteria

1. WHEN tree nodes are displayed THEN the system SHALL show expand/collapse icons for parents
2. WHEN tree nodes are expanded THEN the system SHALL show children indented by 20px
3. WHEN tree nodes are collapsed THEN the system SHALL hide all descendants
4. WHEN tree nodes have icons THEN the system SHALL display them before text at 16px
5. WHEN tree nodes are selected THEN the system SHALL highlight with accent color background
6. WHEN tree nodes support checkboxes THEN the system SHALL show them before icons
7. WHEN parent nodes are checked THEN the system SHALL check all children
8. WHEN some children are checked THEN the system SHALL show parent as indeterminate
9. WHEN tree nodes are draggable THEN the system SHALL support reordering and nesting
10. WHEN tree views are large THEN the system SHALL support virtual scrolling

### Requirement 71: Data Table Components

**User Story:** As a user, I want data tables for structured information, so that I can view and interact with tabular data.

#### Acceptance Criteria

1. WHEN tables are displayed THEN the system SHALL use alternating row backgrounds (zebra striping)
2. WHEN table headers are shown THEN the system SHALL use semibold font and darker background
3. WHEN table rows are hovered THEN the system SHALL highlight with 5-8% opacity background
4. WHEN table columns are sortable THEN the system SHALL show sort icons in headers
5. WHEN table columns are sorted THEN the system SHALL show active sort direction (up/down arrow)
6. WHEN table columns are resizable THEN the system SHALL show resize handle on hover
7. WHEN table rows are selectable THEN the system SHALL show checkboxes in first column
8. WHEN table rows are selected THEN the system SHALL highlight with accent color background
9. WHEN tables have actions THEN the system SHALL show them in last column or on row hover
10. WHEN tables support pagination THEN the system SHALL show controls at bottom
11. WHEN tables support filtering THEN the system SHALL provide filter inputs in headers
12. WHEN tables are responsive THEN the system SHALL stack columns on mobile or provide horizontal scroll

### Requirement 72: Calendar/Date Picker Components

**User Story:** As a user, I want calendar and date picker components, so that I can select dates easily.

#### Acceptance Criteria

1. WHEN date pickers are displayed THEN the system SHALL show input with calendar icon
2. WHEN calendar is opened THEN the system SHALL show current month with day grid
3. WHEN dates are selectable THEN the system SHALL highlight hovered date
4. WHEN dates are selected THEN the system SHALL show them with accent color background
5. WHEN today's date is shown THEN the system SHALL outline it with accent color border
6. WHEN month/year headers are shown THEN the system SHALL provide prev/next navigation
7. WHEN month/year are clickable THEN the system SHALL allow quick selection via dropdown
8. WHEN date ranges are supported THEN the system SHALL highlight all dates between start and end
9. WHEN dates are disabled THEN the system SHALL show them with reduced opacity
10. WHEN calendar supports time THEN the system SHALL provide hour/minute selection

### Requirement 73: File Upload Components

**User Story:** As a user, I want file upload components, so that I can attach files easily.

#### Acceptance Criteria

1. WHEN file upload areas are displayed THEN the system SHALL show dashed border with upload icon
2. WHEN files are dragged over THEN the system SHALL highlight drop zone with accent color
3. WHEN files are dropped THEN the system SHALL show upload progress for each file
4. WHEN files are uploading THEN the system SHALL display progress bar with percentage
5. WHEN files are uploaded THEN the system SHALL show success state with checkmark
6. WHEN upload fails THEN the system SHALL show error state with retry button
7. WHEN files are listed THEN the system SHALL show name, size, and remove button
8. WHEN file types are restricted THEN the system SHALL show accepted formats below drop zone
9. WHEN file size is limited THEN the system SHALL validate and show error for oversized files
10. WHEN multiple files are supported THEN the system SHALL allow selecting/dropping multiple files

### Requirement 74: Search Input Components

**User Story:** As a user, I want search input components, so that I can find content quickly.

#### Acceptance Criteria

1. WHEN search inputs are displayed THEN the system SHALL show search icon on the left
2. WHEN search inputs have text THEN the system SHALL show clear button (×) on the right
3. WHEN search inputs receive focus THEN the system SHALL show accent color border
4. WHEN search is active THEN the system SHALL show loading spinner replacing search icon
5. WHEN search has suggestions THEN the system SHALL show dropdown with autocomplete options
6. WHEN search suggestions are shown THEN the system SHALL highlight matching text
7. WHEN search supports filters THEN the system SHALL provide filter chips below input
8. WHEN search has recent searches THEN the system SHALL show them when input is focused
9. WHEN search supports voice THEN the system SHALL show microphone icon button
10. WHEN search is submitted THEN the system SHALL emit search event with query text

### Requirement 75: Notification/Snackbar Components

**User Story:** As a user, I want notification messages, so that I receive feedback about actions and events.

#### Acceptance Criteria

1. WHEN notifications are shown THEN the system SHALL display them at bottom-center or top-right
2. WHEN notifications appear THEN the system SHALL slide in over 200ms
3. WHEN notifications are temporary THEN the system SHALL auto-dismiss after 3-5 seconds
4. WHEN notifications are persistent THEN the system SHALL require manual dismissal
5. WHEN notifications have actions THEN the system SHALL show action buttons on the right
6. WHEN notifications are success THEN the system SHALL use green color with checkmark icon
7. WHEN notifications are error THEN the system SHALL use red color with error icon
8. WHEN notifications are warning THEN the system SHALL use orange color with warning icon
9. WHEN notifications are info THEN the system SHALL use blue color with info icon
10. WHEN multiple notifications exist THEN the system SHALL stack them with 8px spacing

