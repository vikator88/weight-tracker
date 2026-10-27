"use client";

import ProfileNameFields from "@/presentation/common/components/molecules/profile-name-fields";
import {
  type OnboardingStep,
  type OnboardingStepProps,
  validateNameFields,
} from "@/presentation/onboarding/hooks/use-onboarding";

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
