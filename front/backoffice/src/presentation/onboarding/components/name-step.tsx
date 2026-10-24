"use client";

import ProfileNameFields from "@/presentation/profile/components/profile-name-fields";
import {
  type OnboardingStep,
  type OnboardingStepProps,
  validateNameFields,
} from "../hooks/use-onboarding";

function NameStep({ values, errors, disabled, onChange }: OnboardingStepProps) {
  return (
    <ProfileNameFields values={values} errors={errors} disabled={disabled} onChange={onChange} />
  );
}

/** Step 1: "Dinos cómo te llamas". */
export const nameStep: OnboardingStep = {
  id: "name",
  titleKey: "steps.name.title",
  Body: NameStep,
  validate: validateNameFields,
};

export default NameStep;
