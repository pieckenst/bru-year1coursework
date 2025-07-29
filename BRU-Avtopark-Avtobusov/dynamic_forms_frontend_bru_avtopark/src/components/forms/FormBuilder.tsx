import { useEffect, useState, useCallback } from 'react';
import { SurveyCreator, SurveyCreatorComponent } from 'survey-creator-react';
import 'survey-core/survey-core.css';
import 'survey-creator-core/survey-creator-core.css';
import { formService } from '../../services/api';
import type { FormDefinition } from '../../types/forms';

interface FormBuilderProps {
  formId?: string;
  onSave?: (form: FormDefinition) => void;
  onCancel?: () => void;
  showToolbox?: 'right' | 'left' | 'none';
  showPropertyGrid?: boolean;
  allowEditExpressions?: boolean;
}

export const FormBuilder: React.FC<FormBuilderProps> = ({ 
  formId, 
  onSave, 
  onCancel,
  showToolbox = 'left',
  showPropertyGrid = true,
  allowEditExpressions = true
}) => {
  const [creator, setCreator] = useState<SurveyCreator | null>(null);
  const [isLoading, setIsLoading] = useState(!!formId);
  const [error, setError] = useState<string | null>(null);

  // Default survey JSON schema
  const defaultJson = {
    pages: [{
      name: "page1",
      elements: [{
        type: "text",
        name: "question1",
        title: "Question 1"
      }]
    }]
  };

  // Initialize the form builder
  useEffect(() => {
    const options = {
      showLogicTab: true,
      isAutoSave: false,
      showTranslationTab: true,
      showThemeTab: true,
      showJSONEditorTab: true,
      pageEditMode: "standard" as const,
      showEmbededSurveyTab: true,
      showTitlesInExpressions: true,
      showDefaultLanguageInTestSurveyTab: true,
      showDefaultLanguageInPreviewTab: true,
      showDesignerTab: true,
      showTestSurveyTab: true,
     
      showPropertyGrid: showPropertyGrid,
      haveCommercialLicense: false, // Set to true if you have a commercial license
      toolboxLocation: showToolbox === 'none' ? 'hidden' : showToolbox,
      showToolbox: showToolbox !== 'none'
    };

    const creator = new SurveyCreator(options);
    
    // Set up save functionality
    creator.saveSurveyFunc = async (saveNo: number, callback: (no: number, success: boolean) => void) => {
      try {
        const formData = {
          name: creator.survey.title || `Untitled Form ${new Date().toLocaleDateString()}`,
          description: creator.survey.description || '',
          jsonSchema: creator.text,
          ...(formId ? { isActive: true } : {}) // Only include isActive for updates
        };

        if (formId) {
          const updatedForm = await formService.updateForm(formId, formData);
          onSave?.({
            ...updatedForm,
            name: formData.name,
            description: formData.description || '',
            jsonSchema: formData.jsonSchema
          });
        } else {
          const createdForm = await formService.createForm(formData);
          onSave?.({
            ...createdForm,
            name: formData.name,
            description: formData.description || '',
            jsonSchema: formData.jsonSchema
          });
        }
        
        callback(saveNo, true);
      } catch (error) {
        console.error('Error saving form:', error);
        setError('Failed to save form. Please try again.');
        callback(saveNo, false);
      }
    };

    // Load form data if editing
    const loadForm = async () => {
      if (!formId) {
        creator.text = JSON.stringify(defaultJson, null, 2);
        setIsLoading(false);
        return;
      }

      try {
        const form = await formService.getFormById(formId);
        if (form) {
          creator.text = form.jsonSchema;
          creator.survey.title = form.name;
          creator.survey.description = form.description || '';
        }
      } catch (error) {
        console.error('Error loading form:', error);
        setError('Failed to load form data');
      } finally {
        setIsLoading(false);
      }
    };

    loadForm();
    setCreator(creator);
  }, [formId, onSave, showToolbox, showPropertyGrid, allowEditExpressions]);

  if (isLoading) {
    return <div className="loading">Loading form builder...</div>;
  }

  if (error) {
    return <div className="error">{error}</div>;
  }

  return (
    <div className="form-builder-container">
      {creator && <SurveyCreatorComponent creator={creator} />}
      <style >{`
        .form-builder-container {
          height: 100%;
          min-height: 500px;
        }
        .loading, .error {
          padding: 20px;
          text-align: center;
        }
        .error {
          color: #dc3545;
        }
      `}</style>
    </div>
  );
};