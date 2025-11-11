use chrono::NaiveDate;

/// Format a NaiveDate to DD.MM.YYYY format for UI display
pub fn format_date_for_ui(date: Option<NaiveDate>) -> String {
    date.map(|d| d.format("%d.%m.%Y").to_string())
        .unwrap_or_default()
}

/// Parse a date from DD.MM.YYYY format to NaiveDate
pub fn parse_date_from_ui(date_str: &str) -> Option<NaiveDate> {
    if date_str.trim().is_empty() {
        return None;
    }
    
    NaiveDate::parse_from_str(date_str.trim(), "%d.%m.%Y").ok()
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_format_date() {
        let date = NaiveDate::from_ymd_opt(2025, 1, 15).unwrap();
        assert_eq!(format_date_for_ui(Some(date)), "15.01.2025");
        assert_eq!(format_date_for_ui(None), "");
    }
    
    #[test]
    fn test_parse_date() {
        assert_eq!(
            parse_date_from_ui("15.01.2025"),
            NaiveDate::from_ymd_opt(2025, 1, 15)
        );
        assert_eq!(parse_date_from_ui(""), None);
        assert_eq!(parse_date_from_ui("invalid"), None);
    }
}
