import { useEffect, useState } from 'react';
import { Model } from 'survey-core';

import { Survey } from 'survey-react-ui';
import 'survey-core/survey-core.css';

import { formService } from '../../services/api';
import type { FormDefinition } from '../../types/forms';

interface FormViewerProps {
  formId: string;
  onSubmit?: (data: any) => void;
  onError?: (error: string) => void;
  initialData?: any;
}

export const FormViewer: React.FC<FormViewerProps> = ({
  formId,
  onSubmit,
  onError,
  initialData
}) => {
  const [surveyModel, setSurveyModel] = useState<Model | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<any>(initialData);

  // Load form definition and initialize survey
  useEffect(() => {
    const loadForm = async () => {
      try {
        setIsLoading(true);
        setError(null);
        
        // Fetch form definition
        const form = await formService.getFormById(formId);
        
        // Parse the JSON schema
        const surveyJson = JSON.parse(form.jsonSchema);
        
        // Initialize survey model
        const model = new Model(surveyJson);
        
        // Set initial data if provided
        if (formData) {
          model.data = formData;
        }
        
        // Handle form submission
        model.onComplete.add(async (sender, options) => {
          try {
            // If onSubmit callback is provided, call it with the form data
            if (onSubmit) {
              await onSubmit(sender.data);
            } else {
              // Otherwise, submit to the default endpoint
              await formService.submitForm(formId, sender.data);
              // Show success message
              alert('Form submitted successfully!');
            }
          } catch (err) {
            console.error('Error submitting form:', err);
            const errorMessage = err instanceof Error ? err.message : 'Failed to submit form';
            setError(errorMessage);
            if (onError) {
              onError(errorMessage);
            }
          }
        });
        
        // Handle validation errors
        model.onServerValidateQuestions.add((sender, options) => {
          // You can add custom server-side validation here if needed
          // options.error = 'Custom validation error';
        });
        
        // Handle value changes for dynamic behavior
        model.onValueChanged.add((sender, options) => {
          // You can add dynamic behavior based on field changes here
        });
        
        setSurveyModel(model);
      } catch (err) {
        console.error('Error loading form:', err);
        const errorMessage = 'Failed to load form';
        setError(errorMessage);
        if (onError) {
          onError(errorMessage);
        }
      } finally {
        setIsLoading(false);
      }
    };

    loadForm();
  }, [formId, formData, onError, onSubmit]);

  if (isLoading) {
    return <div>Loading form...</div>;
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        {error}
      </div>
    );
  }

  if (!surveyModel) {
    return <div>Form not found or could not be loaded.</div>;
  }

  return (
    <div className="form-viewer">
      <Survey model={surveyModel} />
      
      <style>{`
        .form-viewer {
          max-width: 800px;
          margin: 0 auto;
          padding: 1rem;
        }
        .alert {
          padding: 0.75rem 1.25rem;
          margin-bottom: 1rem;
          border: 1px solid transparent;
          border-radius: 0.25rem;
        }
        .alert-danger {
          color: #721c24;
          background-color: #f8d7da;
          border-color: #f5c6cb;
        }
      `}</style>
    </div>
  );
};

export default FormViewer;
