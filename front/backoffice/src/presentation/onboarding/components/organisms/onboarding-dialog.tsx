"use client";

import Alert from "@/presentation/common/components/atoms/alert";
import Box from "@mui/material/Box";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import { useTranslation } from "react-i18next";
import useOnboarding, { type OnboardingStep } from "@/presentation/onboarding/hooks/use-onboarding";
import { nameStep } from "@/presentation/onboarding/components/organisms/name-step";

interface OnboardingDialogProps {
  steps?: OnboardingStep[];
}

const productionSteps: OnboardingStep[] = [nameStep];

/** Modal card that cannot be dismissed (`onClose` ignores Escape and backdrop clicks): the user stays here until the last step is saved. */
export default function OnboardingDialog({ steps = productionSteps }: OnboardingDialogProps) {
  const { t } = useTranslation("onboarding");
  const { t: tc } = useTranslation();
  const onboarding = useOnboarding(steps);
  const { Body } = onboarding.step;

  return (
    <Dialog open fullWidth maxWidth="sm" onClose={() => {}} aria-labelledby="onboarding-title">
      <DialogTitle id="onboarding-title">
        {t("stepTitle", {
          current: onboarding.stepNumber,
          total: onboarding.stepCount,
          title: t(onboarding.step.titleKey),
        })}
      </DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {onboarding.errorCode && (
            <Alert severity="error">{tc(`errors:${onboarding.errorCode}`)}</Alert>
          )}
          <Body
            values={onboarding.values}
            errors={onboarding.errors}
            disabled={onboarding.saving}
            onChange={onboarding.change}
          />
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        {!onboarding.isFirst && (
          <Button onClick={onboarding.back} disabled={onboarding.saving}>
            {t("actions.back")}
          </Button>
        )}
        {!onboarding.isLast && (
          <Button variant="contained" onClick={onboarding.next}>
            {t("actions.next")}
          </Button>
        )}
        {onboarding.isLast && (
          <Button
            variant="contained"
            disabled={onboarding.saving}
            onClick={() => void onboarding.finish()}
          >
            {t("actions.finish")}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
