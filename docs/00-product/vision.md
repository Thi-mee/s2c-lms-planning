# Variable LMS product vision

> **Status:** Confirmed baseline\
> **Authority:** Canonical — product direction\
> **Updated:** 2026-09-17

## Purpose

Describe the product and its intended customers. Variable LMS is a product of **Variable**; its initial deployment serves **s2c's internal training**.

## Product

Organizations create reusable text-based courses and deliver them through scheduled cohorts with human coordination and facilitation. Learners receive assigned courses, discuss material, take quizzes, track their own progress and receive completion certificates. Organizations control membership, learning operations and licensed active-learner capacity.

Customer-owned infrastructure and data are central to the product. Variable distributes versioned OCI images and deployment packages, and customers decide when to apply upgrades. On-premises, private-cloud and assisted-operation installations use the same application images. A future vendor-hosted offering is possible, but its tenancy, provisioning and operating model are not an MVP commitment.

The first implementation uses .NET 10 LTS / ASP.NET Core, a modular monolith, React + Vite, and PostgreSQL. Redis is optional. The original Go/single-binary wording is superseded by [accepted decisions D2–D4](accepted-decisions.md).

## Success

- s2c can operate the complete assigned-cohort learning path on infrastructure it controls.
- Account and resource authorization, learner capacity and learning history remain correct under concurrent operations.
- Customers can install, back up, upgrade and recover using versioned documented artifacts.
- Learners can use the responsive web experience to complete text courses and receive reliable credentials.

The earlier 1,000–10,000 active-learner aspiration remains a planning input, not a measured concurrency promise. Concrete hardware, workload and latency/recovery targets are [Q61](open-questions.md#q61). Designing for possible growth does not require services or a 100,000-learner launch commitment.

## Open questions

[Q61](open-questions.md#q61) (operational targets); [Q63](open-questions.md#q63) (first-release support matrix). These do not reopen the accepted architecture.

## Related documents

[Accepted decisions](accepted-decisions.md) · [MVP](../07-roadmap/mvp.md) · [Goals](goals.md) · [Target users](target-users.md)
