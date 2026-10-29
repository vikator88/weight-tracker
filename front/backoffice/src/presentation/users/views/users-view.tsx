"use client";

import { useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import type { UserSummary } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import InactiveFilter from "@/presentation/users/components/molecules/inactive-filter";
import RoleFilter from "@/presentation/users/components/molecules/role-filter";
import ChangeUserActivationDialog, {
  type ActivationChange,
} from "@/presentation/users/components/organisms/change-user-activation-dialog";
import CreateUserDialog from "@/presentation/users/components/organisms/create-user-dialog";
import UsersTable from "@/presentation/users/components/organisms/users-table";
import { filterUsers } from "@/presentation/users/filter-users";
import useUsers from "@/presentation/users/hooks/use-users";
import { userRoles } from "@/presentation/users/roles";

function UsersContent() {
  const { t } = useTranslation("users");
  const current = useCurrentUser();
  const { status, users, errorCode, reload } = useUsers();
  const [search, setSearch] = useState("");
  const [role, setRole] = useState<string | null>(null);
  const [showInactive, setShowInactive] = useState(false);
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState(false);
  const [target, setTarget] = useState<UserSummary | null>(null);
  const [changed, setChanged] = useState<ActivationChange | null>(null);
  // Waits for the caller's profile too, so its failure is shown here like the list's.
  const loaded = status === "loaded" && current.status === "loaded" && current.profile !== null;
  // Only the role decides it, so reloading the list never hides the button.
  const isAdmin = current.status === "loaded" && current.profile?.role === "ADMIN";

  function openCreate() {
    setCreated(false);
    setChanged(null);
    setCreating(true);
  }

  function handleCreated() {
    setCreated(true);
    reload();
  }

  function openActivation(user: UserSummary) {
    setChanged(null);
    setCreated(false);
    setTarget(user);
  }

  function handleActivationChanged(change: ActivationChange) {
    setTarget(null);
    setChanged(change);
    reload();
  }

  function handleActivationClosed(stale: boolean) {
    setTarget(null);
    if (stale) {
      reload();
    }
  }

  function list() {
    if (current.status === "failed" && current.errorCode) {
      return <LoadError code={current.errorCode} onRetry={current.reload} />;
    }
    if (status === "failed" && errorCode) {
      return <LoadError code={errorCode} onRetry={reload} />;
    }
    if (!loaded) {
      return <HomeSkeleton />;
    }
    return (
      <UsersTable
        users={filterUsers(users, search, role, showInactive)}
        filtered={
          search.trim() !== "" ||
          role !== null ||
          (!showInactive && users.some((user) => !user.isActive))
        }
        onToggleActivation={isAdmin ? openActivation : undefined}
        currentUserId={current.profile?.id}
      />
    );
  }

  return (
    <Container sx={{ py: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t("list.title")}
        </Typography>
        {isAdmin && (
          <Button variant="contained" startIcon={<AddOutlined />} onClick={openCreate}>
            {t("list.addUser")}
          </Button>
        )}
      </Box>
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mb: 3 }}>
        <TextField
          type="search"
          label={t("list.search")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ maxWidth: 480 }}
        />
        <Box
          sx={{
            display: "flex",
            flexWrap: "wrap",
            alignItems: "flex-end",
            justifyContent: "space-between",
            gap: 2,
          }}
        >
          <RoleFilter
            label={t("list.profileFilter")}
            options={userRoles.map(({ code }) => ({ code, label: t(`roles.${code}`) }))}
            selected={role}
            onToggle={(code) => setRole((selected) => (selected === code ? null : code))}
          />
          <InactiveFilter
            label={t("list.showInactive")}
            checked={showInactive}
            onChange={setShowInactive}
          />
        </Box>
      </Box>
      {created && (
        <Alert severity="success" onClose={() => setCreated(false)} sx={{ mb: 2 }}>
          {t("create.success")}
        </Alert>
      )}
      {changed && (
        <Alert severity="success" onClose={() => setChanged(null)} sx={{ mb: 2 }}>
          {t(`activation.${changed === "deactivated" ? "deactivate" : "activate"}.success`)}
        </Alert>
      )}
      {list()}
      {isAdmin && (
        <CreateUserDialog
          open={creating}
          onClose={() => setCreating(false)}
          onCreated={handleCreated}
        />
      )}
      {isAdmin && (
        <ChangeUserActivationDialog
          user={target}
          onClose={handleActivationClosed}
          onChanged={handleActivationChanged}
        />
      )}
    </Container>
  );
}

export default function UsersView() {
  return (
    <AppShell>
      <UsersContent />
    </AppShell>
  );
}
