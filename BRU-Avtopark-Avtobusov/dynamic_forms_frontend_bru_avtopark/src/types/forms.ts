export interface FormDefinition {
  id: string;
  name: string;
  description?: string;
  jsonSchema: string;
  createdAt: string;
  updatedAt?: string;
  createdBy: string;
  isActive: boolean;
}

export interface CreateFormDto {
  name: string;
  description?: string;
  jsonSchema: string;
}

export interface UpdateFormDto {
  name?: string;
  description?: string;
  jsonSchema?: string;
  isActive?: boolean;
}
