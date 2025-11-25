# CosmicSegmentedButton Component

## Overview

The `CosmicSegmentedButton` component provides a grouped button control for related options, following COSMIC Desktop design principles. It supports both single and multiple selection modes, horizontal and vertical layouts, and includes proper visual feedback for all interaction states.

## Features

- **Selection Modes**: Single selection (radio-like) or multiple selection (checkbox-like)
- **Layouts**: Horizontal or vertical orientation
- **Visual States**: Normal, hover, pressed, selected, disabled, and focused states
- **Customizable**: Adjustable height, icon size, and divider visibility
- **Accessibility**: Full keyboard navigation support with focus indicators
- **Responsive**: Adapts to different segment counts and content

## Requirements Validated

This component validates the following requirements from the specification:

- **Requirement 23.1-23.5**: Tab bar and segmented button styling
- **Requirement 43.1**: Segments are visually joined with shared borders
- **Requirement 43.2**: Selected segments are highlighted with accent color background
- **Requirement 43.3**: Horizontal layout uses 32px height with center alignment
- **Requirement 43.4**: Vertical layout stacks segments with consistent spacing
- **Requirement 43.5**: Icons are displayed at 16-20px size
- **Requirement 43.6**: Single selection ensures only one segment is active at a time
- **Requirement 43.7**: Multiple selection allows multiple active states

## Usage

### Basic Example (Single Selection)

```slint
import { CosmicSegmentedButton, SegmentItem, SelectionMode, SegmentOrientation } from "cosmic-1.0/cosmic.slint";

component MyView {
    in-out property <[int]> selected: [0];
    
    CosmicSegmentedButton {
        items: [
            { text: "Option 1", icon: @image-url(""), enabled: true },
            { text: "Option 2", icon: @image-url(""), enabled: true },
            { text: "Option 3", icon: @image-url(""), enabled: true },
        ];
        selection-mode: SelectionMode.single;
        orientation: SegmentOrientation.horizontal;
        selected-indices: root.selected;
        
        segment-clicked(index) => {
            root.selected = [index];
        }
    }
}
```

### Multiple Selection Example

```slint
component MyView {
    in-out property <[int]> selected: [0, 2];
    
    CosmicSegmentedButton {
        items: [
            { text: "Bold", icon: @image-url(""), enabled: true },
            { text: "Italic", icon: @image-url(""), enabled: true },
            { text: "Underline", icon: @image-url(""), enabled: true },
        ];
        selection-mode: SelectionMode.multiple;
        orientation: SegmentOrientation.horizontal;
        selected-indices: root.selected;
        
        segment-clicked(index) => {
            // Toggle selection in Rust code
            // This is typically handled in the Rust backend
        }
    }
}
```

### Vertical Layout Example

```slint
component MyView {
    in-out property <[int]> selected: [1];
    
    CosmicSegmentedButton {
        width: 200px;
        items: [
            { text: "Top", icon: @image-url(""), enabled: true },
            { text: "Middle", icon: @image-url(""), enabled: true },
            { text: "Bottom", icon: @image-url(""), enabled: true },
        ];
        selection-mode: SelectionMode.single;
        orientation: SegmentOrientation.vertical;
        selected-indices: root.selected;
        
        segment-clicked(index) => {
            root.selected = [index];
        }
    }
}
```

## Properties

### Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `items` | `[SegmentItem]` | `[]` | Array of segment items to display |
| `selection-mode` | `SelectionMode` | `SelectionMode.single` | Single or multiple selection mode |
| `orientation` | `SegmentOrientation` | `SegmentOrientation.horizontal` | Layout orientation |
| `segment-height` | `length` | `32px` | Height of each segment (can be 32px or 44px) |
| `icon-size` | `length` | `16px` | Size of icons within segments |
| `show-dividers` | `bool` | `true` | Whether to show dividers between segments |

### In-Out Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `selected-indices` | `[int]` | `[]` | Array of currently selected segment indices |

### Callbacks

| Callback | Parameters | Description |
|----------|------------|-------------|
| `segment-clicked` | `int` (index) | Emitted when a segment is clicked |

## Data Structures

### SegmentItem

```slint
struct SegmentItem {
    text: string,      // Text label for the segment
    icon: image,       // Optional icon (use @image-url("") for no icon)
    enabled: bool,     // Whether the segment is enabled
}
```

### SelectionMode

```slint
enum SelectionMode {
    single,    // Only one segment can be selected at a time
    multiple,  // Multiple segments can be selected
}
```

### SegmentOrientation

```slint
enum SegmentOrientation {
    horizontal,  // Segments arranged horizontally
    vertical,    // Segments arranged vertically
}
```

## Selection Management

### Single Selection Mode

In single selection mode, the component behaves like a radio button group:

1. Only one segment can be selected at a time
2. Clicking a segment deselects all others and selects the clicked one
3. The `selected-indices` array should contain exactly one index

**Example handling in Slint:**

```slint
segment-clicked(index) => {
    root.selected-indices = [index];
}
```

### Multiple Selection Mode

In multiple selection mode, the component behaves like a checkbox group:

1. Multiple segments can be selected simultaneously
2. Clicking a segment toggles its selection state
3. The `selected-indices` array can contain multiple indices

**Example handling in Rust:**

```rust
segmented_button.on_segment_clicked(move |index| {
    let mut selected = segmented_button.get_selected_indices();
    
    if let Some(pos) = selected.iter().position(|&x| x == index) {
        // Remove if already selected
        selected.remove(pos);
    } else {
        // Add if not selected
        selected.push(index);
    }
    
    segmented_button.set_selected_indices(selected.into());
});
```

## Styling

The component automatically adapts to the current theme (light/dark mode) and uses:

- **Border Radius**: 8px on outer corners (first and last segments)
- **Border**: 1px solid border using `CosmicPalette.border-neutral`
- **Selected State**: Accent color background at 15% opacity
- **Hover State**: Surface variant background at 50% opacity
- **Pressed State**: Surface variant background darkened by 5%
- **Disabled State**: 40% opacity
- **Focus Indicator**: 2px accent color outline

## Accessibility

- **Keyboard Navigation**: Full keyboard support with Tab/Shift+Tab
- **Focus Indicators**: Clear 2px accent-colored outline when focused
- **Disabled States**: Properly indicated with reduced opacity
- **Screen Readers**: Semantic structure with proper ARIA attributes (when implemented)

## Best Practices

1. **Limit Segment Count**: Keep segments to 3-5 items for optimal usability
2. **Consistent Content**: Use either all text, all icons, or icon+text combinations
3. **Clear Labels**: Use concise, descriptive labels for each segment
4. **Appropriate Height**: Use 32px for compact UIs, 44px for touch-friendly interfaces
5. **Selection Handling**: Always handle the `segment-clicked` callback to update selection state
6. **Disabled Segments**: Use sparingly and provide clear indication why a segment is disabled

## Common Use Cases

1. **View Switchers**: Toggle between different views (List/Grid, Day/Week/Month)
2. **Text Formatting**: Toggle text styles (Bold/Italic/Underline)
3. **Filter Controls**: Select filter options (All/Active/Completed)
4. **Tab Navigation**: Navigate between related content sections
5. **Alignment Controls**: Select alignment options (Left/Center/Right)

## Testing

A comprehensive test file is provided at `segmented-button-test.slint` that demonstrates:

1. Horizontal single selection
2. Horizontal multiple selection
3. Vertical single selection
4. Disabled segments
5. Without dividers
6. Custom height (44px)

Run the test with:

```bash
slint-viewer cosmic-1.0/ui/components/segmented-button-test.slint
```

## Implementation Notes

### Selection State Management

The component uses a helper function `is-selected(index)` that checks if a given index exists in the `selected-indices` array. This function supports up to 10 selected items, which is sufficient for most use cases.

### Border Radius Handling

Individual segments have computed border radii based on their position:
- First segment: Rounded on left (horizontal) or top (vertical)
- Last segment: Rounded on right (horizontal) or bottom (vertical)
- Middle segments: No rounding (square corners)

### Dividers

Dividers are 1px lines placed between segments. They can be hidden by setting `show-dividers: false`.

## Future Enhancements

Potential improvements for future versions:

1. **Icon-Only Mode**: Support segments with only icons (no text)
2. **Close Buttons**: Add optional close buttons for tab-like behavior
3. **Drag-to-Reorder**: Support reordering segments via drag and drop
4. **Overflow Handling**: Scroll or collapse segments when space is limited
5. **Tooltips**: Add tooltip support for segment descriptions
6. **Badge Support**: Display notification badges on segments

## Related Components

- **CosmicButton**: For standalone action buttons
- **CosmicIconButton**: For icon-only buttons
- **CosmicTabBar**: For full-featured tab navigation
- **CosmicRadioButton**: For traditional radio button groups
- **CosmicCheckBox**: For traditional checkbox groups
