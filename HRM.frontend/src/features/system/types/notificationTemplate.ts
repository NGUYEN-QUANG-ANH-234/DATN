export interface TemplateVariable {
  code: string;
  label: string;
  dataType: "Text" | "Textarea" | "Number" | "Money" | "Date" | "DateTime" | "Boolean";
  sourceType: "Manual";
  isRequired: boolean;
  description?: string | null;
  placeholder?: string;
}

export interface NotificationTemplate {
  templateKey: string;
  displayName: string;
  category: string;
  allowedPlaceholders: string[];
  systemPlaceholders: string[];
  customVariables: TemplateVariable[];
  subject: string;
  bodyHtml: string;
}
