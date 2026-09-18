# Frontend guidance

> **Status:** Confirmed routing\
> **Authority:** Supporting\
> **Updated:** 2026-09-18

## Purpose

Guide the React/Vite first-party web application. [Navigation and walking-skeleton screens](navigation.md) is the target UI map. The login/account shell, course library, draft editor, published text view and Course Author access screen are implemented in `src/web`; see [local development](../06-architecture/local-development.md) to run them. Other learning/administration screens remain planned. The separate [public dossier](../../public/README.md) is documentation, not a frontend prototype or product requirement source.

Add a [screen specification](screen-template.md) when a concrete implementation slice needs detail. Link it to canonical features and permissions instead of copying policy into a role-specific layout. Product behavior lives in domain/features; rendering, route names and components are engineering decisions.

## Open questions and related documents

[Question register](../00-product/open-questions.md) · [API guidance](../04-api-design/README.md) · [User journeys](../01-domain/user-journeys.md) · [ADR-8](../06-architecture/decisions.md#adr-8-frontend--react-spa-vite-no-ssr)
