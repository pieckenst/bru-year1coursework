# COSMIC Text Input Components

This document describes the text input components implemented for the COSMIC Desktop styling system.

## Components

### 1. CosmicTextInput

A single-line text input component with comprehensive features.

**Requirements:** 8.1-8.5, 56.1-56.9

**Features:**
- 32px height standard input
- Label above input (14px semibold)
- Focus state with 2px accent border
- Error state with red border and error message
- Disabled state (40% opacity)
- Placeholder with secondary color
- Clear button on hover when text is present
- Character counter with max length enforcement

**Properties:**
```slint
in-out property <string> text;              // Input text value
in property <string> placeholder;           // Placeholder text
in property <string> label;                 // Label above input
in property <bool> disabled: false;         // Disabled state
in property <bool> has-error: false;        // Error state
in property <string> error-message;         // Error message text
in property <int> max-length: -1;           // Max character limit (-1 = no limit)
in property <bool> show-counter: false;     // Show character counter
in property <bool> show-clear-button: true; // Show clear button
```

**Callbacks:**
```slint
callback edited(string);   // Fired when text changes
callback accepted(string); // Fired when Enter is pressed
callback cleared();        // Fired when clear button is clicked
```

**Example:**
```slint
CosmicTextInput {
    label: "Email";
    placeholder: "user@example.com";
    text: email-text;
    has-error: !email-text.contains("@");
    error-message: "Please enter a valid email";
    max-length: 100;
    show-counter: true;
}
```

### 2. CosmicTextArea

A multi-line text input component for longer text entry.

**Requirements:** 56.8

**Features:**
- Multi-line text input with minimum 3 rows
- Auto-resize functionality
- Character counter
- Same states as text input (focus, error, disabled)
- Scrollable content area

**Properties:**
```slint
in-out property <string> text;          // Input text value
in property <string> placeholder;       // Placeholder text
in property <string> label;             // Label above input
in property <bool> disabled: false;     // Disabled state
in property <bool> has-error: false;    // Error state
in property <string> error-message;     // Error message text
in property <int> min-rows: 3;          // Minimum number of rows
in property <int> max-rows: -1;         // Maximum rows (-1 = no limit)
in property <bool> auto-resize: true;   // Auto-resize based on content
in property <int> max-length: -1;       // Max character limit
in property <bool> show-counter: false; // Show character counter
```

**Callbacks:**
```slint
callback edited(string);   // Fired when text changes
callback accepted(string); // Fired when Ctrl+Enter is pressed
```

**Example:**
```slint
CosmicTextArea {
    label: "Description";
    placeholder: "Enter a detailed description...";
    text: description-text;
    min-rows: 4;
    max-length: 500;
    show-counter: true;
}
```

### 3. CosmicSearchInput

A specialized search input with icon, loading state, and autocomplete.

**Requirements:** 56.10, 74.1-74.7

**Features:**
- Search icon on left (16px)
- Clear button on right
- Loading spinner state
- Autocomplete dropdown
- Suggestion highlighting
- Keyboard navigation support

**Properties:**
```slint
in-out property <string> text;                      // Search text
in property <string> placeholder: "Search...";      // Placeholder text
in property <string> label;                         // Label above input
in property <image> search-icon;                    // Search icon image
in property <length> icon-size: 16px;               // Icon size
in property <bool> disabled: false;                 // Disabled state
in property <bool> loading: false;                  // Loading state
in property <[SearchSuggestion]> suggestions;       // Autocomplete suggestions
in property <bool> show-suggestions: false;         // Show dropdown
in property <int> selected-suggestion-index: -1;    // Selected suggestion
```

**Structs:**
```slint
export struct SearchSuggestion {
    text: string,       // Suggestion text
    highlighted: bool,  // Whether to highlight (bold)
}
```

**Callbacks:**
```slint
callback edited(string);            // Fired when text changes
callback accepted(string);          // Fired when Enter is pressed
callback cleared();                 // Fired when clear button is clicked
callback suggestion-selected(int);  // Fired when suggestion is clicked
```

**Example:**
```slint
CosmicSearchInput {
    label: "Search Users";
    placeholder: "Type to search...";
    text: search-text;
    loading: is-searching;
    show-suggestions: search-text != "";
    suggestions: [
        { text: "Alice Johnson", highlighted: false },
        { text: "Bob Smith", highlighted: true },
        { text: "Charlie Brown", highlighted: false },
    ];
    
    edited(new-text) => {
        search-text = new-text;
        // Trigger search...
    }
    
    suggestion-selected(index) => {
        // Handle selection...
    }
}
```

## Design Tokens Used

All components use the COSMIC Desktop design system tokens:

**Colors:**
- `CosmicPalette.background-surface` - Input background
- `CosmicPalette.text-primary` - Input text
- `CosmicPalette.text-secondary` - Placeholder and icons
- `CosmicPalette.accent-primary` - Focus border
- `CosmicPalette.border-neutral` - Default border
- `CosmicPalette.error` - Error state

**Typography:**
- `CosmicTypography.input-size` (16px) - Input text
- `CosmicTypography.label-size` (14px) - Labels
- `CosmicTypography.caption-size` (12px) - Error messages and counters

**Spacing:**
- `CosmicSpacing.input-padding-horizontal` (12px)
- `CosmicSpacing.input-padding-vertical` (8px)
- `CosmicSpacing.space-xxxs` (4px) - Vertical spacing

**Border Radius:**
- `CosmicCornerRadii.input-radius` (8px)

## Accessibility

All components follow WCAG 2.1 AA guidelines:

- Minimum contrast ratio of 4.5:1 for text
- Clear focus indicators (2px accent border)
- Keyboard navigation support
- Disabled state with reduced opacity
- Error messages with semantic color coding
- Labels properly associated with inputs

## States

All components support the following states:

1. **Normal** - Default appearance
2. **Hover** - Subtle visual feedback
3. **Focus** - 2px accent color border
4. **Disabled** - 40% opacity, no interaction
5. **Error** - Red border with error message
6. **Loading** (SearchInput only) - Spinner replaces icon

## Testing

See `examples/text-input-example.slint` for a comprehensive demonstration of all components and their features.

To run the example:
```bash
cargo run
```

## Implementation Notes

- Character counting uses Slint's `character-count` property for proper Unicode support
- Clear button appears on hover or focus when text is present
- Focus state uses 2px border instead of 1px for better visibility
- Error messages appear below the input with appropriate spacing
- Character counters turn red when limit is exceeded
- Search suggestions dropdown has max height of 8 items with scrolling
- All components use consistent padding and spacing from the design system
