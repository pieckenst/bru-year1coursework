import type { FormDefinition, CreateFormDto, UpdateFormDto } from '../types/forms';

const API_BASE_URL = 'http://localhost:5000/api';

const handleResponse = async (response: Response) => {
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || 'Something went wrong');
  }
  return response.json();
};

export const formService = {
  // Get all forms
  getAllForms: async (): Promise<FormDefinition[]> => {
    const response = await fetch(`${API_BASE_URL}/forms`, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
    });
    return handleResponse(response);
  },

  // Get a single form by ID
  getFormById: async (id: string): Promise<FormDefinition> => {
    const response = await fetch(`${API_BASE_URL}/forms/${id}`, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
    });
    return handleResponse(response);
  },

  // Create a new form
  createForm: async (formData: CreateFormDto): Promise<FormDefinition> => {
    const response = await fetch(`${API_BASE_URL}/forms`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
      body: JSON.stringify(formData),
    });
    return handleResponse(response);
  },

  // Update an existing form
  updateForm: async (id: string, formData: UpdateFormDto): Promise<FormDefinition> => {
    const response = await fetch(`${API_BASE_URL}/forms/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
      body: JSON.stringify(formData),
    });
    return handleResponse(response);
  },

  // Delete a form (soft delete)
  deleteForm: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/forms/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to delete form');
    }
  },

  // Submit form data
  submitForm: async (formId: string, formData: any): Promise<any> => {
    const response = await fetch(`${API_BASE_URL}/forms/${formId}/submit`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
      body: JSON.stringify({ data: formData }),
    });
    return handleResponse(response);
  },
};
