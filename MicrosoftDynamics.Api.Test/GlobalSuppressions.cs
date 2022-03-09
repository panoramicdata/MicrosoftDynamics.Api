// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System;
using System.Diagnostics.CodeAnalysis;

[assembly: CLSCompliant(false)]
[assembly: SuppressMessage(
	"Naming",
	"CA1707:Identifiers should not contain underscores",
	Justification = "Appropriate for unit tests",
	Scope = "namespaceanddescendants",
	Target = "~N:MicrosoftDynamics.Api.Test")]
