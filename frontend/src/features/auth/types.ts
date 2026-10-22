export interface UserBrief {
  /** ExternalId (Guid) serialized as string. */
  id: string;
  username: string;
  fullName: string;
  email: string;
  roles: string[];
}

export interface AuthResult {
  token: string;
  user: UserBrief;
}
