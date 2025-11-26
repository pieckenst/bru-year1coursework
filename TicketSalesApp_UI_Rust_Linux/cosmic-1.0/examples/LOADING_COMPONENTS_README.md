# COSMIC Loading Components Example

This example demonstrates the loading and progress indicator components in the COSMIC Desktop design system.

## Components Demonstrated

### 1. CosmicSpinner
A circular rotating loading indicator with three size variants:
- **Small (16px)**: For inline contexts like text or small buttons
- **Medium (32px)**: For buttons and medium-sized containers
- **Large (64px)**: For full-page loading states

**Features:**
- Smooth continuous rotation animation
- Accent color by default (customizable)
- Configurable stroke width based on size
- 270-degree arc for visual interest

### 2. CosmicProgressBar
A linear progress indicator with two modes:
- **Determinate**: Shows specific progress value (0.0 to 1.0)
- **Indeterminate**: Shows animated loading without specific value

**Features:**
- 4px height with rounded ends
- Accent color fill
- Smooth animations
- Indeterminate mode with sliding animation

### 3. CosmicCircularProgress
A circular progress indicator with percentage display:
- Shows progress as a circular arc
- Optional percentage text in center
- Configurable size and stroke width
- Accent color by default

**Features:**
- Progress from 0% to 100%
- Dynamic arc calculation
- Centered percentage display
- Smooth visual feedback

### 4. CosmicSkeletonLoader
Pulsing placeholder shapes for loading content:
- **Text**: Line placeholders for text content
- **Circle**: Circular placeholders (e.g., avatars)
- **Rectangle**: Rectangular placeholders (e.g., images, cards)

**Features:**
- Pulsing opacity animation
- Shimmer effect overlay
- Multiple shape variants
- Convenience components (SkeletonText, SkeletonCircle, SkeletonRectangle)

## Running the Example

To run this example:

```bash
slint-viewer examples/loading-components-example.slint
```

Or if using the Rust application:

```bash
cargo run --example loading-components
```

## Usage Examples

### Spinner in Button
```slint
import { CosmicButton } from "../ui/components/button.slint";

CosmicButton {
    text: "Loading...";
    loading: true;  // Shows spinner automatically
}
```

### Progress Bar for File Upload
```slint
import { CosmicProgressBar, ProgressMode } from "../ui/components/progress-bar.slint";

CosmicProgressBar {
    mode: ProgressMode.determinate;
    progress: upload-progress; // 0.0 to 1.0
}
```

### Circular Progress for Downloads
```slint
import { CosmicCircularProgress } from "../ui/components/circular-progress.slint";

CosmicCircularProgress {
    progress: download-progress;
    show-percentage: true;
    size: 64px;
}
```

### Skeleton Loader for Content Loading
```slint
import { SkeletonText, SkeletonCircle, SkeletonRectangle } from "../ui/components/skeleton-loader.slint";

// Loading state for a user card
VerticalLayout {
    HorizontalLayout {
        SkeletonCircle { circle-size: 48px; }
        VerticalLayout {
            SkeletonText { text-width: 120px; }
            SkeletonText { text-width: 80px; }
        }
    }
    SkeletonRectangle { rect-width: 300px; rect-height: 200px; }
}
```

## Design Guidelines

### When to Use Each Component

**Spinner:**
- Use for indeterminate loading states
- Best for short waits (< 10 seconds)
- Use small size inline with text
- Use medium size in buttons
- Use large size for full-page loading

**Progress Bar:**
- Use when progress can be measured
- Best for file uploads, downloads, multi-step processes
- Use indeterminate mode when progress is unknown
- Keep visible throughout the operation

**Circular Progress:**
- Use when space is limited
- Best for showing percentage completion
- Use in dashboards and status displays
- Provides more visual weight than linear progress

**Skeleton Loader:**
- Use for initial page loads
- Best for content-heavy interfaces
- Maintains layout structure during loading
- Reduces perceived loading time
- Use shapes that match final content

### Accessibility

All loading components follow COSMIC accessibility guidelines:
- Sufficient color contrast (accent color meets WCAG AA)
- Smooth animations (can be disabled via system preferences)
- Semantic meaning through visual design
- Consider adding ARIA labels for screen readers

### Performance

- Animations use CSS/GPU acceleration where possible
- Skeleton loaders are lightweight placeholders
- Spinners use simple path rendering
- Progress bars update efficiently

## Requirements Validation

These components satisfy the following requirements:

**Requirement 61.1-61.5**: Spinner component with three sizes and accent color
**Requirement 61.6**: Circular progress indicator with percentage display
**Requirement 61.7-61.8**: Progress bar with determinate and indeterminate modes
**Requirement 61.10**: Skeleton loader with pulsing animation and multiple shapes

## Customization

All components support customization:

```slint
// Custom colored spinner
CosmicSpinner {
    color: CosmicPalette.success;
}

// Custom progress bar colors
CosmicProgressBar {
    color: CosmicPalette.info;
    track-color: CosmicPalette.surface-variant;
}

// Custom skeleton colors
CosmicSkeletonLoader {
    base-color: CosmicPalette.surface-variant;
    shimmer-color: CosmicPalette.surface-variant.lighter(0.15);
}
```
