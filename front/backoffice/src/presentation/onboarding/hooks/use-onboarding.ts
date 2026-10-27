import { useState, type ComponentType } from "react";
import { validateUpdateProfileRequest } from "@weight-tracker/core/application";
import type {
  ProfileNameErrors,
  ProfileNameValues,
} from "@/presentation/common/components/molecules/profile-name-fields";
import useUpdateProfile from "@/presentation/profile/hooks/use-update-profile";

export interface OnboardingStepProps {
  values: ProfileNameValues;
  errors: ProfileNameErrors;
  disabled: boolean;
  onChange: (field: keyof ProfileNameValues, value: string) => void;
}

export interface OnboardingStep {
  id: string;
  /** Key of the step title in the `onboarding` namespace. */
  titleKey: string;
  Body: ComponentType<OnboardingStepProps>;
  validate: (values: ProfileNameValues) => ProfileNameErrors;
}

/** Violations of the name and surname fields, leaving out the valid ones. */
export function validateNameFields(values: ProfileNameValues): ProfileNameErrors {
  const violations = validateUpdateProfileRequest(values);
  const errors: ProfileNameErrors = {};

  if (violations.name) {
    errors.name = violations.name;
  }
  if (violations.surname) {
    errors.surname = violations.surname;
  }
  return errors;
}

/** Walks an ordered list of steps; the last one saves what every step collected. */
export default function useOnboarding(steps: OnboardingStep[]) {
  const { saving, errorCode, save } = useUpdateProfile();
  const [index, setIndex] = useState(0);
  const [values, setValues] = useState<ProfileNameValues>({ name: "", surname: "" });
  const [errors, setErrors] = useState<ProfileNameErrors>({});
  const step = steps[index];

  function validateStep() {
    const found = step.validate(values);
    setErrors(found);
    return Object.keys(found).length === 0;
  }

  function change(field: keyof ProfileNameValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
  }

  function next() {
    if (index < steps.length - 1 && validateStep()) {
      setIndex(index + 1);
    }
  }

  function back() {
    if (index > 0) {
      setErrors({});
      setIndex(index - 1);
    }
  }

  async function finish() {
    if (validateStep()) {
      await save({ name: values.name.trim(), surname: values.surname.trim() });
    }
  }

  return {
    step,
    stepNumber: index + 1,
    stepCount: steps.length,
    isFirst: index === 0,
    isLast: index === steps.length - 1,
    values,
    errors,
    saving,
    errorCode,
    change,
    next,
    back,
    finish,
  };
}
