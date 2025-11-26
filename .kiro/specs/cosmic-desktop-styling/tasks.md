# Implementation Plan

## Phase 1: Foundation (Tasks 1-2)

- [x] 1. Create cosmic-1.0 Directory Structure





  - Create cosmic-1.0 directory at project root
  - Copy Inter font files from material-1.0/font to cosmic-1.0/font
  - Create ui/styling, ui/components, ui/icons subdirectories
  - Create cosmic.slint as main entry point
  - _Requirements: 15.1, 15.2_

- [x] 2. Implement Theme System Foundation





  - _Requirements: 1.1-1.5, 3.1-3.5, 6.1-6.5, 5.1-5.5, 37.1-37.8, 38.1-38.8, 39.1-39.7_

- [x] 2.1 Create palette.slint with comprehensive color system


  - Define CosmicPalette global with light/dark mode colors
  - Add accent color configuration (brownish #8B7355, orange #FF6B35)
  - Add semantic colors (error, success, warning, info)
  - Implement computed colors based on dark-mode boolean
  - Add semantic color naming (accent-primary, text-primary, background-base, etc.)
  - _Requirements: 1.1-1.5, 2.1-2.5, 13.1-13.5, 14.1-14.5, 39.1-39.7_

- [ ]* 2.2 Write property test for theme consistency
  - **Property 2: Theme consistency across components**
  - **Validates: Requirements 1.1-1.5, 13.1-13.5, 14.1-14.5**

- [x] 2.3 Create typography.slint with complete font system


  - Define CosmicTypography global with Inter font family
  - Add font size scale (12px, 14px, 16px, 18px, 20px, 24px, 29px, 32px, 35px)
  - Add font weight definitions (300, 400, 500, 600, 700)
  - Add line height definitions (17px, 21px, 30px, 36px, 43px, 52px)
  - Add typography presets (title1-4, heading, body, caption, caption-heading)
  - _Requirements: 3.1-3.5, 25.1-25.8_

- [ ]* 2.4 Write property test for typography consistency
  - **Property 3: Typography scale consistency**
  - **Validates: Requirements 3.1-3.5, 25.1-25.8**

- [x] 2.5 Create spacing.slint with comprehensive spacing scale


  - Define CosmicSpacing global with 4px-based increments
  - Add spacing values (xxxs: 4px, xxs: 8px, xs: 12px, s: 16px, m: 24px, l: 32px, xl: 48px, xxl: 64px)
  - _Requirements: 6.1-6.5, 37.1-37.8_

- [ ]* 2.6 Write property test for spacing consistency
  - **Property 4: Spacing scale consistency**
  - **Validates: Requirements 6.1-6.5, 37.1-37.8**

- [x] 2.7 Create corner-radii.slint with radius system


  - Define CosmicCornerRadii global with radius scale
  - Add radius values (0, xs: 4px, s: 8px, m: 12px, l: 16px, xl: 20px, xxl: 24px, full: 9999px)
  - _Requirements: 4.1-4.5, 38.1-38.8_

- [ ]* 2.8 Write property test for border radius consistency
  - **Property 5: Border radius consistency**
  - **Validates: Requirements 4.1-4.5, 38.1-38.8**

- [x] 2.9 Create elevation.slint with shadow system


  - Define CosmicElevation global with shadow definitions
  - Add sm, md, lg shadow levels with offsets and blur
  - Adjust shadow colors for light/dark modes
  - _Requirements: 5.1-5.5_

- [ ]* 2.10 Write property test for elevation consistency
  - **Property 12: Shadows adapt to elevation level**
  - **Validates: Requirements 5.1-5.5**

## Phase 2: Core Button Components (Tasks 3-5)


- [x] 3. Implement Core Button Components





  - _Requirements: 7.1-7.8, 29.1-29.5, 30.1-30.7, 31.1-31.6, 32.1-32.6, 33.1-33.7, 34.1-34.7_

- [x] 3.1 Create button.slint with CosmicButton base


  - Define CosmicButton with text, icon, variant, class properties
  - Add size presets (xs, sm, md, lg, xl) with appropriate dimensions
  - Implement TouchArea for interaction
  - Add disabled, selected, loading states
  - _Requirements: 7.1-7.4, 29.1-29.5_

- [x] 3.2 Implement button variants and states

  - Add standard variant (accent background, white text)
  - Add suggested variant (enhanced accent)
  - Add destructive variant (red/error color)
  - Add text-only variant (transparent background)
  - Implement hover state (darken 10-15%)
  - Implement pressed state (darken 15%, reduce shadow)
  - Implement focused state (2px accent outline)
  - Implement disabled state (40% opacity)
  - Implement loading state (spinner, disabled interaction)
  - _Requirements: 7.1-7.8, 33.1-33.7, 34.1-34.7_

- [ ]* 3.3 Write property test for button state transitions
  - **Property 8: Button state transitions**
  - **Validates: Requirements 7.5, 7.8, 34.1-34.7**

- [x] 3.4 Create icon-button.slint with CosmicIconButton


  - Implement icon-only button (40x40px standard)
  - Support symbolic (16px) and regular (24px) icons
  - Add vertical/horizontal label layouts
  - Add selected state with accent color
  - Add tooltip support
  - _Requirements: 7.6, 12.3, 12.4, 30.1-30.7_

- [ ]* 3.5 Write property test for icon button sizing
  - **Property 17: Icon size consistency**
  - **Validates: Requirements 12.3, 12.4, 30.1-30.2**



- [x] 3.6 Create link-button.slint with CosmicLinkButton
  - Implement link-style button with accent text
  - Add underline on hover
  - Add trailing icon support (external-link)


  - Add focus outline (2px accent)
  - _Requirements: 32.1-32.6_

- [x] 3.7 Create image-button.slint with CosmicImageButton
  - Implement image-based button with 9px radius
  - Add selection indicator at bottom-left
  - Add remove icon (×) at top-right on hover
  - Add hover overlay effect
  - _Requirements: 31.1-31.6_

- [x] 4. Implement Segmented Button Component



  - _Requirements: 23.1-23.5, 43.1-43.7_

- [x] 4.1 Create segmented-button.slint

  - Implement grouped button control with shared borders
  - Support single/multiple selection modes
  - Add horizontal/vertical layouts
  - Add selected state (accent background)
  - Add dividers between segments
  - _Requirements: 23.1-23.5, 43.1-43.7_

- [ ]* 4.2 Write property test for segmented button selection
  - **Property: Segmented button selection modes**
  - **Validates: Requirements 43.6-43.7**

- [x] 5. Implement Menu Button Components





  - _Requirements: 21.1-21.7, 42.1-42.8_



- [x] 5.1 Create menu-button.slint

  - Implement button with dropdown arrow
  - Add menu item component (36px height)
  - Add keyboard shortcut display
  - Add submenu support with arrow indicator
  - Add checkbox menu items with checkmark
  - Add menu dividers
  - _Requirements: 21.1-21.7, 42.1-42.8_

- [ ]* 5.2 Write property test for menu keyboard navigation
  - **Property: Menu keyboard navigation**
  - **Validates: Requirements 42.3, 53.3**

## Phase 3: Input Components (Tasks 6-10)

- [x] 6. Implement Text Input Components





  - _Requirements: 8.1-8.5, 56.1-56.10_



- [x] 6.1 Create text-input.slint with CosmicTextInput
  - Implement 32px height single-line input
  - Add label above input (14px semibold)
  - Add focus state (2px accent border)
  - Add error state (red border, error message)
  - Add disabled state (40% opacity)
  - Add placeholder with secondary color
  - Add clear button on hover
  - Add character counter
  - _Requirements: 8.1-8.5, 56.1-56.9_

- [ ]* 6.2 Write property test for input focus state
  - **Property 7: Input focus state shows accent color**
  - **Validates: Requirements 8.2, 56.3**

- [ ]* 6.3 Write property test for input error display
  - **Property 9: Input validation error display**


  - **Validates: Requirements 8.5, 56.4**

- [x] 6.2 Create text-area.slint with CosmicTextArea
  - Implement multi-line text input (minimum 3 rows)
  - Add auto-resize functionality


  - Add character counter
  - Apply same states as text input
  - _Requirements: 56.8_

- [x] 6.3 Create search-input.slint with CosmicSearchInput
  - Add search icon on left (16px)
  - Add clear button on right
  - Add loading spinner state
  - Add autocomplete dropdown
  - Add suggestion highlighting
  - _Requirements: 56.10, 74.1-74.7_

- [x] 7. Implement Selection Input Components







  - _Requirements: 57.1-57.10, 58.1-58.10_

- [x] 7.1 Create checkbox.slint with CosmicCheckBox


  - Implement 20px × 20px checkbox with 4px radius
  - Add checked state (checkmark, accent background)
  - Add indeterminate state (minus icon)
  - Add label with 8px spacing
  - Add disabled state (40% opacity)
  - Add focus outline (2px accent)
  - Support parent checkbox for select-all
  - _Requirements: 57.1-57.10_

- [ ]* 7.2 Write property test for checkbox states
  - **Property: Checkbox state management**
  - **Validates: Requirements 57.2, 57.3, 57.9**




- [x] 7.3 Create radio-button.slint with CosmicRadioButton


  - Implement 20px circular radio button
  - Add selected state (filled inner circle, accent)
  - Add label with 8px spacing
  - Add disabled state (40% opacity)
  - Add focus outline (2px accent)
  - Ensure only one selection per group
  - _Requirements: 57.5-57.10_




- [X] 7.4 Create toggle.slint with CosmicToggle
  - Implement 24px height pill-shaped toggle
  - Add off state (neutral background)
  - Add on state (accent background)
  - Animate knob transition (150ms)
  - Add label with 8px spacing
  - Add disabled state (40% opacity)
  - Add loading state (spinner on knob)
  - Add description text below
  - _Requirements: 58.1-58.10_

- [ ]* 7.5 Write property test for toggle state synchronization
  - **Property 19: Toggle state synchronization**
  - **Validates: Requirements 58.1-58.4**

- [-] 8. Implement Slider Components


  - _Requirements: 59.1-59.10_

- [x] 8.1 Create slider.slint with CosmicSlider


  - Implement 4px track height
  - Add accent-colored progress fill
  - Add 16px circular thumb
  - Add dragging state (20px thumb, shadow)
  - Add min/max labels at track ends
  - Add value label above thumb
  - Add tick marks below track
  - Add disabled state (40% opacity)
  - Support vertical orientation
  - _Requirements: 59.1-59.10_

- [x] 8.2 Create range-slider.slint with CosmicRangeSlider



  - Implement dual-thumb range slider
  - Add min/max thumb controls
  - Add range highlight between thumbs
  - Apply same styling as single slider
  - _Requirements: 59.9_

- [ ]* 8.3 Write property test for slider value constraints
  - **Property: Slider value within min/max bounds**
  - **Validates: Requirements 59.1-59.2**

- [x] 9. Implement Dropdown Components




  - _Requirements: 60.1-60.10_


- [x] 9.1 Create dropdown.slint with CosmicDropdown
  - Implement 32px height dropdown with down arrow
  - Add options list (max 8 visible items)
  - Add hover highlight on options
  - Add selected state with checkmark
  - Add search input at top
  - Add multi-select with checkboxes
  - Add group headers with dividers
  - Add disabled state (40% opacity)
  - Add scrolling for long lists
  - Add icon support before option text
  - _Requirements: 60.1-60.10_

- [ ]* 9.2 Write property test for dropdown viewport containment
  - **Property 15: Popover viewport containment**
  - **Validates: Requirements 24.4, 24.5, 60.9**


- [x] 10. Implement Loading Components





  - _Requirements: 61.1-61.10_

- [x] 10.1 Create spinner.slint with CosmicSpinner


  - Implement circular rotating animation
  - Add small size (16px) for inline contexts
  - Add medium size (32px) for buttons
  - Add large size (64px) for page loading
  - Apply accent color to rotating arc
  - _Requirements: 61.1-61.5_

- [x] 10.2 Create progress-bar.slint with CosmicProgressBar


  - Implement 4px height with rounded ends
  - Add determinate mode (fill with accent color)
  - Add indeterminate mode (animated fill)
  - _Requirements: 61.7-61.8_

- [x] 10.3 Create circular-progress.slint


  - Implement circular progress indicator
  - Add percentage display option
  - Apply accent color to progress arc
  - _Requirements: 61.6_



- [x] 10.4 Create skeleton-loader.slint
  - Implement pulsing placeholder shapes
  - Add shimmer animation effect
  - Support various shapes (text, circle, rectangle)
  - _Requirements: 61.10_

- [ ]* 10.5 Write property test for loading state visibility
  - **Property: Loading indicators show during async operations**
  - **Validates: Requirements 61.1-61.10**

## Phase 4: Display Components (Tasks 11-15)

- [x] 11. Implement Card and Container Components



  - _Requirements: 18.1-18.5, 35.1-35.7, 48.1-48.7_

- [x] 11.1 Create card.slint with CosmicCard


  - Implement elevated surface with subtle shadow
  - Add title with medium font weight
  - Add content area with 16-20px padding
  - Add interactive variant with hover effects
  - Add border radius (12-16px)
  - Increase shadow on hover (sm to md)
  - _Requirements: 18.1-18.5, 48.1-48.7_

- [ ]* 11.2 Write property test for card hover elevation
  - **Property 13: Interactive cards increase elevation on hover**
  - **Validates: Requirements 5.3, 18.4**

- [x] 11.2 Create container.slint with CosmicContainer


  - Implement generic container with padding
  - Support background layer colors
  - Add border support (1px neutral)
  - Add elevation option (subtle shadow)
  - _Requirements: 35.1-35.7_

- [x] 11.3 Create divider.slint with CosmicDivider



  - Implement horizontal/vertical separator (1px)
  - Add optional label centered with spacing
  - Adjust color for light/dark modes
  - _Requirements: 36.1-36.6_

- [-] 12. Implement Badge and Chip Components


  - _Requirements: 62.1-62.10_

- [x] 12.1 Create badge.slint with CosmicBadge


  - Implement 20px height with 10px radius
  - Add count display with accent background
  - Position at top-right of parent
  - Display "99+" for counts over 99
  - Add semantic color variants
  - _Requirements: 62.1-62.4, 62.10_


- [ ] 12.2 Create chip.slint with CosmicChip



  - Implement 28px height with full radius
  - Add removable variant with close icon (×)
  - Add avatar support (24px on left)
  - Add clickable variant with hover state
  - Support inline display with wrapping
  - _Requirements: 62.5-62.9_

- [ ] 13. Implement Avatar Components
  - _Requirements: 63.1-63.10_

- [ ] 13.1 Create avatar.slint with CosmicAvatar
  - Implement circular shape by default
  - Add size variants (xs: 24px, sm: 32px, md: 40px, lg: 56px)
  - Add initials fallback with colored background
  - Add status indicator dot at bottom-right
  - Add square variant with 8px radius
  - Add clickable variant with hover border
  - _Requirements: 63.1-63.10_

- [ ] 13.2 Create avatar-group.slint
  - Implement overlapping avatars (-8px margin)
  - Add overflow count display
  - Support click events on individual avatars
  - _Requirements: 63.8_

- [ ] 14. Implement Tooltip Component
  - _Requirements: 64.1-64.10_

- [ ] 14.1 Create tooltip.slint with CosmicTooltip
  - Implement dark background with white text
  - Add 500ms hover delay
  - Add arrow pointing to trigger
  - Add viewport repositioning
  - Use 12px font size
  - Add fade out animation (150ms)
  - Support rich content formatting
  - Add touch device support (tap to show)
  - Set max width to 300px with text wrapping
  - Ensure only one tooltip visible at a time
  - _Requirements: 64.1-64.10_

- [ ]* 14.2 Write property test for tooltip positioning
  - **Property: Tooltip viewport containment**
  - **Validates: Requirements 64.4**

- [ ] 15. Implement Alert Components
  - _Requirements: 47.1-47.7_

- [ ] 15.1 Create alert.slint with CosmicAlert
  - Implement warning color background (#F39C12)
  - Add close button on right side
  - Ensure sufficient text contrast
  - Add icon support (16px with spacing)
  - Add dismissible variant with close button
  - Add persistent variant without close
  - Stack multiple alerts with spacing
  - _Requirements: 47.1-47.7_

## Phase 5: Layout Components (Tasks 16-20)

- [ ] 16. Implement App Bar and Header Components
  - _Requirements: 16.1-16.7, 22.1-22.7, 45.1-45.8_

- [ ] 16.1 Create app-bar.slint with CosmicAppBar
  - Implement surface background with minimal elevation
  - Add title (18-20px, semibold)
  - Add action buttons (40x40px, 8px radius)
  - Add hover effect (surface-variant background)
  - Set padding (24px horizontal, 16-20px vertical)
  - Add menu toggle icon support
  - _Requirements: 16.1-16.7_


- [ ] 16.2 Create header-bar.slint with CosmicHeaderBar
  - Implement 32-40px height header
  - Add window control buttons (minimize, maximize, close)
  - Use symbolic icons at 16px
  - Add maximize/restore icon toggle
  - Add focused/unfocused opacity (100%/75%)
  - Add draggable area for window movement
  - Support double-click to toggle maximize
  - Support start, center, end content regions
  - _Requirements: 22.1-22.7, 45.1-45.8_

- [ ]* 16.3 Write property test for header bar drag functionality
  - **Property: Header bar emits drag-requested callback**
  - **Validates: Requirements 22.7**

- [ ] 17. Implement Navigation Components
  - _Requirements: 11.1-11.7, 17.1-17.5, 41.1-41.7_

- [ ] 17.1 Create drawer.slint with CosmicDrawer
  - Implement navigation sidebar with surface-variant background
  - Add icon + text layout (20-24px icons, 12px spacing)
  - Set item height to 44px with 8px radius
  - Add normal state (transparent background)
  - Add hover state (surface 50% opacity)
  - Add active state (accent 15% opacity, accent text, medium weight)
  - Add section headers and dividers
  - Add full height with smooth transitions
  - _Requirements: 11.1-11.7, 17.1-17.5_

- [ ]* 17.2 Write property test for navigation active state
  - **Property 15: Navigation items show active state**
  - **Validates: Requirements 11.4, 17.4**

- [ ] 17.2 Create navigation-bar.slint
  - Implement 32px button height navigation
  - Support icon-only and icon+text modes
  - Add selected state highlighting
  - Add scrolling for many items
  - Support context menus on right-click
  - Support drag-drop reordering
  - Add collapsed mode (icons only)
  - _Requirements: 41.1-41.7_

- [ ] 18. Implement Tab Components
  - _Requirements: 23.1-23.5, 44.1-44.7_

- [ ] 18.1 Create tab-bar.slint with CosmicTabBar
  - Implement 44px height tabs
  - Set width range (76-250px)
  - Add active indicator (accent color below tab)
  - Add close buttons (show on hover/active)
  - Support drag-to-reorder
  - Add overflow scrolling/menu
  - Add icon support before text
  - _Requirements: 23.1-23.5, 44.1-44.7_

- [ ]* 18.2 Write property test for tab selection
  - **Property: Tab selection shows accent indicator**
  - **Validates: Requirements 44.2**

- [ ] 19. Implement Breadcrumb Component
  - _Requirements: 68.1-68.10_

- [ ] 19.1 Create breadcrumb.slint with CosmicBreadcrumb
  - Implement separator display ("/" or ">")
  - Add clickable items with hover underline
  - Add collapsible middle items ("...")
  - Use secondary color for separators
  - Display current page without link styling
  - Add icon support (16px before text)
  - Add mobile mode (last 2 items only)
  - Truncate long items with ellipsis (150px)
  - Add dropdown for collapsed items
  - Use semantic HTML (nav with aria-label)
  - _Requirements: 68.1-68.10_


- [ ] 20. Implement Stepper Component
  - _Requirements: (implied from navigation patterns)_

- [ ] 20.1 Create stepper.slint with CosmicStepper
  - Implement step indicator for multi-step processes
  - Add completed/active/upcoming states
  - Add step numbers and labels
  - Add connecting lines between steps
  - Use accent color for completed/active steps
  - _Requirements: (navigation and progress indication)_

## Phase 6: Overlay Components (Tasks 21-24)

- [ ] 21. Implement Dialog Component
  - _Requirements: 10.1-10.5, 65.1-65.10_

- [ ] 21.1 Create dialog.slint with CosmicDialog
  - Implement backdrop (50% opacity overlay)
  - Center dialog in viewport
  - Add title (20px bold font in header)
  - Add close button at top-right
  - Add action buttons at bottom-right (primary rightmost)
  - Implement focus trap within dialog
  - Add scale animation (0.95 to 1.0, 200ms)
  - Support Escape key to close
  - Fix header/footer for scrollable content
  - Support sizes (small: 400px, medium: 600px, large: 800px)
  - _Requirements: 10.1-10.5, 65.1-65.10_

- [ ]* 21.2 Write property test for dialog focus trap
  - **Property 11: Modal dialog focus trap**
  - **Validates: Requirements 26.4, 26.5, 65.6**

- [ ]* 21.3 Write property test for dialog backdrop interaction
  - **Property 14: Dialog backdrop blocks interaction**
  - **Validates: Requirements 10.1, 65.2**

- [ ] 22. Implement Popover Component
  - _Requirements: 24.1-24.5, 46.1-46.7_

- [ ] 22.1 Create popover.slint with CosmicPopover
  - Position relative to trigger element
  - Support modal mode (block underlying interaction)
  - Support non-modal mode (close on outside click)
  - Implement viewport containment repositioning
  - Add backdrop fade-in (200ms)
  - Emit close event on dismissal
  - Manage z-index hierarchy for multiple popovers
  - _Requirements: 24.1-24.5, 46.1-46.7_

- [ ]* 22.2 Write property test for popover positioning
  - **Property 15: Popover viewport containment**
  - **Validates: Requirements 24.4, 24.5, 46.4**

- [ ] 23. Implement Menu Components
  - _Requirements: 21.1-21.7, 42.1-42.8, 52.1-52.7_

- [ ] 23.1 Create menu.slint with CosmicMenu
  - Implement dropdown menu with items
  - Add submenu support with arrow indicator
  - Add dividers between sections
  - Add keyboard shortcut display (right-aligned)
  - Add checkbox menu items with checkmark
  - Add disabled state (40% opacity)
  - Position adaptively within viewport
  - Add icon support (14-16px before label)
  - _Requirements: 21.1-21.7, 42.1-42.8_

- [ ] 23.2 Create context-menu.slint
  - Implement right-click menu
  - Position at cursor location
  - Reposition if exceeds viewport
  - Close on outside click
  - Execute action and close on selection
  - Open submenus on hover
  - _Requirements: 52.1-52.7_


- [ ] 24. Implement Drawer/Sidebar Components
  - _Requirements: 66.1-66.10_

- [ ] 24.1 Create sidebar.slint with CosmicSidebar
  - Implement slide-in from edges (left/right/top/bottom)
  - Add animation transition (300ms ease-out)
  - Add backdrop (50% opacity)
  - Support modal mode (block main content)
  - Support non-modal mode (push content aside)
  - Fix header at top with close button
  - Fix footer at bottom
  - Support sizes (narrow: 256px, standard: 400px, wide: 600px)
  - Slide out and remove from DOM on close
  - Use full width on mobile
  - _Requirements: 66.1-66.10_

## Phase 7: List and Table Components (Tasks 25-27)

- [ ] 25. Implement List Components
  - _Requirements: 9.1-9.5, 67.1-67.10_

- [ ] 25.1 Create list-item.slint with CosmicListItem
  - Implement selection and hover states
  - Add alternating backgrounds (zebra striping, 3-5% opacity)
  - Add hover highlight (5-8% opacity)
  - Add selected state (accent 10-15% opacity)
  - _Requirements: 9.1-9.5_

- [ ]* 25.2 Write property test for list hover feedback
  - **Property 9: List items provide hover feedback**
  - **Validates: Requirements 9.2**

- [ ]* 25.3 Write property test for list selection state
  - **Property 10: Selected list items show accent color**
  - **Validates: Requirements 9.3**

- [ ] 25.2 Create list.slint with CosmicList
  - Implement scrollable list container
  - Add virtual scrolling for large datasets (1000+ items)
  - Support keyboard navigation
  - _Requirements: (performance optimization)_

- [ ] 25.3 Create tree-view.slint with CosmicTreeView
  - Implement hierarchical tree structure
  - Add expand/collapse icons for parent nodes
  - Add 20px indentation for children
  - Add icon support (16px before text)
  - Add selection with accent color
  - Add checkbox support with parent/child sync
  - Support drag-drop reordering and nesting
  - Add virtual scrolling for large trees
  - _Requirements: 70.1-70.10_

- [ ] 25.4 Create accordion.slint with CosmicAccordion
  - Implement expandable content sections
  - Add expand/collapse icons (rotate 90° on expand)
  - Animate height transition (200ms)
  - Toggle expansion on header click
  - Support single expansion mode (collapse others)
  - Support multiple expansion mode
  - Add borders (1px neutral)
  - Add hover highlight on headers
  - Add disabled state
  - Add 16px indentation for nested items
  - _Requirements: 67.1-67.10_

- [ ] 26. Implement Table Components
  - _Requirements: 9.4-9.5, 71.1-71.12_

- [ ] 26.1 Create data-table.slint with CosmicDataTable
  - Implement alternating row backgrounds (zebra striping)
  - Add header with semibold font and darker background
  - Add row hover highlight (5-8% opacity)
  - Add sortable columns with sort icons
  - Add active sort direction indicators (up/down arrow)
  - Add resizable columns with resize handle
  - Add row selection with checkboxes
  - Add selected row highlighting (accent background)
  - Add action column or row hover actions
  - Add pagination controls at bottom
  - Add filter inputs in headers
  - Support responsive behavior (stack columns or horizontal scroll)
  - _Requirements: 9.4-9.5, 71.1-71.12_


- [ ]* 26.2 Write property test for table zebra striping
  - **Property 20: Table row zebra striping**
  - **Validates: Requirements 9.1, 71.1**

- [ ] 27. Implement Pagination Component
  - _Requirements: 69.1-69.10_

- [ ] 27.1 Create pagination.slint with CosmicPagination
  - Implement page numbers with prev/next buttons
  - Highlight current page with accent color
  - Show first, last, and ellipsis for many pages
  - Disable prev on first page
  - Disable next on last page
  - Navigate to page on number click
  - Add page size dropdown (items per page)
  - Display "Showing 1-10 of 100" text
  - Add compact mobile version (prev/next only)
  - Support jump-to-page input
  - _Requirements: 69.1-69.10_

## Phase 8: Date/Time and File Components (Tasks 28-29)

- [ ] 28. Implement Date/Time Components
  - _Requirements: 72.1-72.10_

- [ ] 28.1 Create date-picker.slint with CosmicDatePicker
  - Implement input with calendar icon
  - Add calendar popup with current month
  - Add day grid with selectable dates
  - Highlight hovered date
  - Show selected date with accent background
  - Outline today's date with accent border
  - Add prev/next month navigation
  - Add month/year dropdown for quick selection
  - Support date range with start/end highlighting
  - Add disabled dates with reduced opacity
  - _Requirements: 72.1-72.9_

- [ ] 28.2 Create time-picker.slint with CosmicTimePicker
  - Implement hour/minute selection
  - Support 12/24-hour formats
  - Add increment/decrement buttons
  - _Requirements: 72.10_

- [ ] 28.3 Create date-time-picker.slint
  - Combine date and time selection
  - Use CosmicDatePicker and CosmicTimePicker
  - _Requirements: 72.10_

- [ ] 29. Implement File Components
  - _Requirements: 73.1-73.10_

- [ ] 29.1 Create file-upload.slint with CosmicFileUpload
  - Implement dashed border drop zone with upload icon
  - Highlight drop zone on drag-over (accent color)
  - Show upload progress for each file
  - Display progress bar with percentage
  - Show success state with checkmark
  - Show error state with retry button
  - List files with name, size, and remove button
  - Show accepted formats below drop zone
  - Validate file size and show error for oversized
  - Support multiple file selection/dropping
  - _Requirements: 73.1-73.10_

## Phase 9: Application Integration (Tasks 30-35)

- [ ] 30. Implement Settings Panel Pattern
  - _Requirements: 40.1-40.8_

- [ ] 30.1 Create settings-panel.slint
  - Implement two-column layout (label left, control right)
  - Add section titles (16px semibold)
  - Add section descriptions (14px regular)
  - Add row labels (14px with optional description)
  - Add row descriptions (12px secondary color)
  - Use space-m spacing between sections
  - Use space-s spacing between rows
  - Align controls to right side
  - _Requirements: 40.1-40.8_


- [ ] 31. Update Application Views to Use cosmic-1.0
  - _Requirements: 15.3, 15.4_

- [ ] 31.1 Update auth_window.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace button components with CosmicButton
  - Replace input components with CosmicTextInput
  - Test login flow
  - _Requirements: 15.3, 15.4_

- [ ] 31.2 Update app-window.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace app bar with CosmicAppBar
  - Replace drawer with CosmicDrawer
  - Test navigation
  - _Requirements: 15.3, 15.4_

- [ ] 31.3 Update bus_management.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace list items with CosmicListItem
  - Replace buttons with CosmicButton
  - Replace dialogs with CosmicDialog
  - Test CRUD operations
  - _Requirements: 15.3, 15.4_

- [ ] 31.4 Update route_management.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace components with COSMIC equivalents
  - Test CRUD operations
  - _Requirements: 15.3, 15.4_

- [ ] 31.5 Update route_schedules.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace components with COSMIC equivalents
  - Test schedule management
  - _Requirements: 15.3, 15.4_

- [ ] 31.6 Update employee_dialogs.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace dialog components with CosmicDialog
  - Replace form inputs with CosmicTextInput
  - Test dialog interactions
  - _Requirements: 15.3, 15.4_

- [ ] 31.7 Update jobs_management.slint
  - Change import from material-1.0 to cosmic-1.0
  - Replace components with COSMIC equivalents
  - Test jobs CRUD operations
  - _Requirements: 15.3, 15.4_

- [ ] 32. Implement Theme Switching
  - _Requirements: 13.1-13.5, 14.1-14.5_

- [ ] 32.1 Add light/dark mode toggle to settings
  - Create settings view with mode toggle
  - Add callback to update dark-mode property
  - Persist mode preference
  - Test mode switching across all views
  - _Requirements: 13.1-13.5, 14.1-14.5_

- [ ]* 32.2 Write property test for theme consistency
  - **Property 1: Theme consistency across components**
  - **Validates: Requirements 1.1-1.5, 13.1-13.5, 14.1-14.5**

- [ ]* 32.3 Write property test for dark mode color inversion
  - **Property 11: Dark mode inverts color scheme**
  - **Validates: Requirements 14.1-14.5**

- [ ] 33. Implement Accent Color Switcher
  - _Requirements: 2.1-2.5_

- [ ] 33.1 Add accent color picker to settings
  - Create color picker UI with presets
  - Add brownish (#8B7355) preset button
  - Add orange (#FF6B35) preset button
  - Add custom color input
  - Test accent color changes across all components
  - _Requirements: 2.1-2.5_

- [ ]* 33.2 Write property test for accent color propagation
  - **Property 2: Accent color propagation**
  - **Validates: Requirements 2.1-2.4**


- [ ] 34. Implement Responsive Behavior
  - _Requirements: 20.1-20.5, 28.1-28.7, 50.1-50.7_

- [ ] 34.1 Add responsive menu bar
  - Implement hamburger menu collapse for narrow windows
  - Show "open-menu-symbolic" icon button
  - Display all menu items in dropdown when collapsed
  - Expand back to full menu bar when window widens
  - Emit size-changed events
  - Use 150px item width in collapsed dropdown
  - Maintain all functionality including submenus
  - _Requirements: 28.1-28.7_

- [ ] 34.2 Add responsive navigation
  - Collapse navigation to icons-only at narrow widths
  - Hide text labels when space is insufficient
  - Restore full navigation when window widens
  - _Requirements: 20.5, 50.5_

- [ ]* 34.3 Write property test for responsive layout
  - **Property 12: Responsive navigation collapse**
  - **Validates: Requirements 20.5, 28.1-28.7, 50.5**

- [ ] 35. Implement Focus Management
  - _Requirements: 26.1-26.5, 53.1-53.7, 54.1-54.7_

- [ ] 35.1 Add focus indicators to all interactive elements
  - Implement 2px accent color outline
  - Animate focus transitions smoothly
  - Add 2px offset for buttons
  - Add outline around input borders
  - Ensure visibility against all backgrounds
  - _Requirements: 26.1-26.2, 54.1-54.7_

- [ ]* 35.2 Write property test for focus indicator visibility
  - **Property 6: Focus indicator visibility**
  - **Validates: Requirements 26.1, 26.2, 54.1-54.7**

- [ ] 35.2 Implement keyboard navigation
  - Support Tab/Shift+Tab for focus movement
  - Support arrow keys in menus
  - Support Enter for activation
  - Support Escape for cancel/close
  - Support Space for checkbox toggle
  - Execute keyboard shortcuts
  - _Requirements: 53.1-53.7_

- [ ]* 35.3 Write property test for keyboard navigation
  - **Property 14: Keyboard navigation sequence**
  - **Validates: Requirements 19.5, 53.1-53.2**

## Phase 10: Testing and Polish (Tasks 36-40)

- [ ] 36. Accessibility Testing
  - _Requirements: 19.1-19.5_

- [ ] 36.1 Test color contrast ratios
  - Use automated tools for all text/background pairs
  - Ensure 4.5:1 ratio for normal text
  - Ensure 3:1 ratio for large text
  - Fix any failing combinations
  - _Requirements: 19.1_

- [ ]* 36.2 Write property test for contrast compliance
  - **Property 13: Accessibility contrast ratio**
  - **Validates: Requirements 19.1**

- [ ] 36.2 Test focus indicators
  - Verify all interactive elements show focus state
  - Ensure visibility in both light and dark modes
  - Test with keyboard navigation
  - _Requirements: 19.2_

- [ ] 36.3 Test keyboard navigation
  - Verify all functionality accessible via keyboard
  - Test logical tab order
  - Test Escape key closes dialogs
  - _Requirements: 19.5_

- [ ] 36.4 Test screen reader compatibility
  - Add ARIA labels to icon-only buttons
  - Add aria-describedby for error messages
  - Test with screen reader software
  - _Requirements: 19.3_


- [ ] 37. Visual Regression Testing
  - _Requirements: All_

- [ ] 37.1 Screenshot all components in light mode
  - Capture button variants and states
  - Capture input states
  - Capture cards, lists, tables
  - Capture app bar, drawer, dialogs
  - Capture all form components
  - _Requirements: 13.1-13.5_

- [ ] 37.2 Screenshot all components in dark mode
  - Capture same components as light mode
  - Verify colors are properly inverted
  - _Requirements: 14.1-14.5_

- [ ] 37.3 Test with brownish accent color
  - Apply brownish accent (#8B7355)
  - Capture screenshots of key components
  - Verify accent is applied consistently
  - _Requirements: 2.1_

- [ ] 37.4 Test with orange accent color
  - Apply orange accent (#FF6B35)
  - Capture screenshots of key components
  - Verify accent is applied consistently
  - _Requirements: 2.1_

- [ ] 38. Responsive Behavior Testing
  - _Requirements: 20.1-20.5_

- [ ] 38.1 Test at minimum window size (320px)
  - Verify all content is accessible
  - Verify no layout breaks
  - Verify navigation collapses appropriately
  - _Requirements: 20.1, 20.2, 50.2_

- [ ] 38.2 Test at maximum window size
  - Verify spacing scales appropriately
  - Verify content doesn't become too spread out
  - _Requirements: 20.1, 50.3_

- [ ] 38.3 Test dialog responsiveness
  - Open dialogs at various window sizes
  - Verify dialogs scale to fit
  - Verify content remains readable
  - _Requirements: 20.4_

- [ ] 39. Performance Testing
  - _Requirements: All_

- [ ] 39.1 Benchmark component rendering
  - Measure time to render each component type
  - Ensure under 16ms (60fps)
  - Identify any performance bottlenecks
  - Optimize if necessary
  - _Requirements: All_

- [ ] 39.2 Test with large datasets
  - Load bus management with 1000+ items
  - Load route management with 1000+ items
  - Verify scrolling remains smooth
  - Test virtual scrolling performance
  - _Requirements: All_

- [ ] 39.3 Test theme switching performance
  - Measure theme switch completion time
  - Ensure under 200ms
  - Test memory usage during switches
  - _Requirements: 13.1-13.5, 14.1-14.5_

- [ ] 40. Final Integration Testing
  - _Requirements: All_

- [ ] 40.1 Test bus management workflow
  - Complete add, edit, delete operations
  - Test search and filter
  - Verify all dialogs work correctly
  - _Requirements: All_

- [ ] 40.2 Test route management workflow
  - Complete add, edit, delete operations
  - Test dropdown selections
  - Verify all dialogs work correctly
  - _Requirements: All_

- [ ] 40.3 Test route schedules workflow
  - Complete add, edit, delete operations
  - Test multi-select for stops
  - Test date picker
  - Verify all dialogs work correctly
  - _Requirements: All_


- [ ] 40.4 Test employee management workflow
  - Complete add, edit, delete operations
  - Test all form inputs
  - Verify all dialogs work correctly
  - _Requirements: All_

- [ ] 40.5 Test jobs management workflow
  - Complete add, edit, delete operations
  - Test search functionality
  - Verify all dialogs work correctly
  - _Requirements: All_

## Phase 11: Documentation and Cleanup (Tasks 41-42)

- [ ] 41. Documentation
  - _Requirements: All_

- [ ] 41.1 Write component API documentation
  - Document all component properties
  - Document all callbacks
  - Document styling options
  - Create usage examples for each component
  - _Requirements: All_

- [ ] 41.2 Create component showcase
  - Build interactive component gallery
  - Show all variants and states
  - Provide code examples
  - _Requirements: All_

- [ ] 41.3 Document theme customization
  - Explain how to change accent colors
  - Explain how to customize spacing
  - Explain how to add new color schemes
  - Provide migration guide from material-1.0
  - _Requirements: 2.1, 2.2, 6.1_

- [ ] 42. Cleanup and Polish
  - _Requirements: All_

- [ ] 42.1 Remove material-1.0 directory (optional)
  - Verify all views use cosmic-1.0
  - Backup material-1.0 if needed
  - Delete material-1.0 directory
  - _Requirements: 15.1_

- [ ] 42.2 Code cleanup
  - Remove any debug code
  - Remove unused imports
  - Format all code consistently
  - Run linter and fix issues
  - _Requirements: All_

- [ ] 42.3 Final code review
  - Review all changes
  - Verify coding standards
  - Check for any remaining issues
  - Verify all requirements are met
  - _Requirements: All_

## Summary

**Total Tasks: 42 major tasks with 150+ subtasks**

**Component Coverage:**
- Foundation: 4 components (palette, typography, spacing, elevation)
- Buttons: 8 components (button, icon-button, link-button, image-button, segmented-button, menu-button, split-button, FAB)
- Inputs: 10 components (text-input, text-area, search-input, password-input, number-input, checkbox, radio, toggle, slider, range-slider)
- Selection: 5 components (dropdown, combobox, select, multi-select, autocomplete)
- Display: 8 components (card, badge, chip, avatar, avatar-group, tooltip, label, divider)
- Feedback: 7 components (spinner, progress-bar, circular-progress, skeleton-loader, alert, toast, snackbar)
- Layout: 9 components (app-bar, header-bar, drawer, navigation-bar, tab-bar, breadcrumb, stepper, spacer, container)
- Overlay: 6 components (dialog, popover, menu, context-menu, bottom-sheet, sidebar)
- List: 5 components (list-item, list, tree-view, accordion, expansion-panel)
- Table: 4 components (data-table, table-header, table-row, table-cell)
- Date/Time: 4 components (date-picker, time-picker, date-time-picker, date-range-picker)
- File: 2 components (file-upload, file-input)
- Pagination: 2 components (pagination, items-per-page)
- Utility: 2 components (focus-indicator, scrollbar)

**Total: 76 components covering all 74 requirements**

**Testing Approach:**
- Optional property-based tests marked with `*` for faster MVP
- Core functionality tests integrated into implementation tasks
- Comprehensive integration testing in Phase 10
- Visual regression testing for both light/dark modes
- Accessibility compliance testing (WCAG 2.1 AA)
- Performance benchmarking for large datasets

**Implementation Strategy:**
- Phase-by-phase approach for manageable progress
- Foundation first (theme system)
- Core components next (buttons, inputs)
- Layout and overlay components
- Application integration
- Testing and polish
- Documentation and cleanup

If doing examples,place them in separate examples folder

Each task references specific requirements for traceability and includes clear implementation guidance.
