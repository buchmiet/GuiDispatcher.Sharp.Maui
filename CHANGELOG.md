# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.1] - 2026-07-24

### Added

- `MauiGuiDispatcher`, backed by `Microsoft.Maui.Dispatching.IDispatcher`.
- Synchronous and asynchronous invocation with exception propagation.
- Explicit failure when the native dispatcher rejects an operation.
- `MauiGuiTimer` and cancelable one-shot scheduling backed by MAUI timers.
- Tests running against a dedicated dispatcher thread and timer implementation.

