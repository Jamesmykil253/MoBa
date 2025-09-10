# xAI Grok API — Expert Guides Companion (Prompt-Engineering Edition)
_Last updated: 2025-09-05 18:20 ._

> **What this is:** An expert-written companion to the official xAI Guides. It synthesizes the Guides’ intent and augments them with PhD-level prompt-engineering patterns, implementation tactics, and guardrails for production systems.  
> **What this is not:** A verbatim copy of xAI’s documentation. For authoritative wording and updates, follow the source links in each section.

---

## Table of Contents
- [Chat](#chat)
- [Chat with Reasoning](#chat-with-reasoning)
- [Live Search](#live-search)
- [Image Understanding](#image-understanding)
- [Image Generations](#image-generations)
- [Streaming Response](#streaming-response)
- [Deferred Chat Completions](#deferred-chat-completions)
- [Asynchronous Requests](#asynchronous-requests)
- [Function Calling](#function-calling)
- [Structured Outputs](#structured-outputs)
- [Fingerprint](#fingerprint)
- [Migration from Other Providers](#migration-from-other-providers)
- [Prompt Engineering for `grok-code-fast-1`](#prompt-engineering-for-grok-code-fast-1)
- [Appendix: Prompt Patterns & Checklists](#appendix-prompt-patterns--checklists)

---

## Chat
**Source:** https://docs.x.ai/docs/guides/chat

The Chat endpoint is the *workhorse* API: text-in, text-out with `messages=[{role, content}]`. Treat it as a stateless function—persist and re-send relevant history yourself. Roles are flexible: you can interleave `system`, `user`, and `assistant` in any order to sculpt behavior and carry context.

### Production tips
- **Contract-first system message.** Pin style, objective, safety/compliance, and output format expectations up front. Keep it stable to leverage caching.
- **Context curation.** Only include *task-relevant* prior turns. Use a sliding window or retrieval to stay within context limits.
- **Evaluate with rubrics.** Add an *internal* evaluation rubric (e.g., correctness, coverage, brevity) and ask the model to self-check before finalizing.

### Minimal requests
```bash
curl -s https://api.x.ai/v1/chat/completions \
  -H "Authorization: Bearer $XAI_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "grok-3-latest",
    "messages": [
      {"role":"system","content":"You are a precise, citation-friendly assistant."},
      {"role":"user","content":"Give a 3-bullet overview of diffusion models."}
    ]
  }'
```

### Prompt-engineering quick pattern
```
SYSTEM: Role | Constraints | Output contract
USER: Task + domain context + inputs
(Optionally) ASSISTANT: exemplar I/O pairs
USER: “Before answering, run a short self-check against the rubric; then produce FINAL.”
```

---

## Chat with Reasoning
**Source:** https://docs.x.ai/docs/guides/reasoning

Reasoning-designated models (e.g., `grok-3-mini`, `grok-3-mini-fast`, and `grok-4`) optimize for stepwise problem solving. Some expose `reasoning_content` (chain-of-thought traces) in responses; `grok-4` does **not** expose it. `presencePenalty`, `frequencyPenalty`, and `stop` are not supported on reasoning models. `reasoning_effort` controls depth (`low` vs `high`; not supported by `grok-4`).

### Engineering guidance
- **Latency vs accuracy dial.** Use `reasoning_effort="low"` for UI snappiness; switch to `high` for adjudication/evaluation passes.
- **Show your work—selectively.** Retain internal traces for audits but avoid leaking them to end users unless explicitly desired.
- **Programmatic verification.** For math/logic, request structured intermediate artifacts (equations, subgoals) and verify downstream.

### Snippet
```jsonc
{
  "model": "grok-3-mini",
  "messages": [{"role":"user","content":"Compute 101*3 and explain briefly."}],
  "reasoning_effort": "high"
}
```

---

## Live Search
**Source:** https://docs.x.ai/docs/guides/live-search

Enable the model to ground answers with fresh data via `search_parameters`. Modes: `"off"`, `"on"`, `"auto"` (model decides). Allow sources (Web/News/X/RSS), constrain domains/date windows, and optionally return citations. Track `usage.num_sources_used` to manage cost.

### Governance & costing
- **Least privilege.** Prefer `allowed_websites` or handles allowlists. Disable unsafe sources for regulated workloads.
- **Determinism over recall.** Cap `max_search_results`; request citations for auditability.
- **Cache + fuse.** Merge retrieved facts into *explicit* context blocks; ask the model to reconcile conflicts and cite.

### Minimal payload
```jsonc
{
  "model": "grok-3-latest",
  "messages": [{"role":"user","content":"Summarize today’s EU AI news with links."}],
  "search_parameters": {
    "mode": "on",
    "sources": [{ "type": "news", "country": "EU" }],
    "return_citations": true,
    "max_search_results": 8
  }
}
```

---

## Image Understanding
**Source:** https://docs.x.ai/docs/guides/image-understanding

Send images via publicly reachable URLs or Base64 within the standard `messages` schema (interleave text and images). Control preprocessing with `"detail": "auto" | "low" | "high"` (speed/cost vs fidelity).

**Token model:** Images are tiled at **448×448**; **256 tokens** per tile, plus **one extra tile** overhead. Max **6 tiles** ⇒ **<1,792 tokens** per image. Use `detail="low"` when you only need coarse structure.

### Example (URL + text in one message)
```jsonc
{
  "role": "user",
  "content": [
    {"type":"text","text":"Extract the axes and trend inflection points."},
    {"type":"image_url","image_url":{"url":"https://example.com/chart.png","detail":"high"}}
  ]
}
```

### Vision prompting tips
- Tag regions (“Top-left plot…”) and enumerate requested outputs (captions, OCR fields, measurements).
- Ask for **uncertainty** and **failure modes** (blur, occlusion) to reduce hallucination risk.

---

## Image Generations
**Source:** https://docs.x.ai/docs/guides/image-generations

Text→image uses a separate endpoint `/v1/images/generations` and model (e.g., `grok-2-image`). Supply `prompt`, optionally `n` (1–10) and `response_format` (`url` or `b64_json`). The system revises your prompt internally and returns `revised_prompt` for transparency.

```bash
curl -s https://api.x.ai/v1/images/generations \
  -H "Authorization: Bearer $XAI_API_KEY" -H "Content-Type: application/json" \
  -d '{"model":"grok-2-image","prompt":"foggy harbor at dawn, long exposure","n":2,"response_format":"url"}'
```

**Authoring prompts:** Describe subject, composition, lighting, lens/medium, era, and negative constraints. Prefer nouns/adjectives over style-only prompts.

---

## Streaming Response
**Source:** https://docs.x.ai/docs/guides/streaming-response

All text-generating models stream via **SSE** when `"stream": true`. Reasoning models may “think” before emitting tokens—raise client timeouts accordingly.

### Client pattern (Python SDK pseudo)
```python
for response, chunk in chat.stream():
    if chunk.content: print(chunk.content, end="", flush=True)
# final response has full message + any citations/usage
```

**UX practice:** Display partial tokens, but avoid committing side effects until `finish_reason="stop"`. Buffer tool calls—these arrive as whole chunks.

---

## Deferred Chat Completions
**Source:** https://docs.x.ai/docs/guides/deferred-chat-completions

Submit work, get a `response_id`, and fetch the final result later (once) within ~24 hours. Useful for slow generations or queue-based backends.

**Flow:** POST (defer) ➜ poll `GET /v1/chat/deferred-completion/{response_id}` ➜ 200 returns the same shape as a normal completion. Handle 202 = not-ready; implement backoff. Record once; results are one-time retrievable.

---

## Asynchronous Requests
**Source:** https://docs.x.ai/docs/guides/async

Use `AsyncClient` (or OpenAI-compatible async clients) to fan out concurrent calls. No batch endpoint—so *orchestrate* with semaphores and backpressure.

### Pattern
- Shard workloads.
- Constrain concurrency to stay under rate limits.
- Retry idempotently on transient errors (timeouts, 429). Tag requests with correlation IDs.

---

## Function Calling
**Source:** https://docs.x.ai/docs/guides/function-calling

Let the model request **tool invocations** with typed parameters; you execute and return results as `role:"tool"` messages, then the model finalizes user-facing text. Modes: `tool_choice`: `"auto" | "required" | {"type":"function","function":{"name":...}} | "none"`. Parallel tool calls are supported.

### Skeleton loop
```python
resp = client.chat.complete(model="grok-4", messages=msgs, tools=tools, tool_choice="auto")
if resp.tool_calls:
    for call in resp.tool_calls:
        result = TOOLMAP[call.name](**call.arguments)
        msgs.append({"role":"tool","tool_call_id": call.id, "content": json.dumps(result)})
    resp = client.chat.complete(model="grok-4", messages=msgs)  # final
```

### Prompt-engineering for tools
- **Schema-first.** Define tools with Pydantic/Zod; include descriptions examples and constraints.
- **Guardrails.** Validate/escape inputs; bound effects; apply allowlists for network or filesystem.
- **Teach when to use.** In the system prompt: decision rules + cost hints.

---

## Structured Outputs
**Source:** https://docs.x.ai/docs/guides/structured-outputs

Constrain responses to match a schema (JSON types: string/number/object/array/boolean/enum/anyOf; `allOf` not supported). Supported on language models newer than `grok-2-1212` (and `grok-2-vision-1212` for vision). Guarantees **type-safe** outputs that match your schema.

### Pattern (Python, Pydantic)
```python
class Item(BaseModel):
    name: str
    qty: int
schema = Item.model_json_schema()
resp = client.chat.complete(model="grok-3", messages=msgs, output_schema=schema)
data = Item.model_validate_json(resp.content)  # type-safe
```

**Design tips:** Normalize enums; encode units; add `"explanation"` fields for auditing (can be optional).

---

## Fingerprint
**Source:** https://docs.x.ai/docs/guides/fingerprint

Every response includes a `system_fingerprint`—log it alongside token/cost metrics to correlate behavior changes with backend updates and aid incident response. Treat fingerprint changes as soft version bumps in your evaluation dashboards.

---

## Migration from Other Providers
**Source:** https://docs.x.ai/docs/guides/migration

xAI’s API is compatible with OpenAI/Anthropic SDKs for core flows. Steps:
1) Point SDK base URL to `https://api.x.ai/v1` and use your xAI key.  
2) Swap model names to Grok variants.  
LangChain/Continue typically work by changing provider + key settings.

---

## Prompt Engineering for `grok-code-fast-1`
**Source:** https://docs.x.ai/docs/guides/grok-code-prompt-engineering

`grok-code-fast-1` is a lightweight **agentic** coding model (fast, low-cost) tuned for tool-rich code navigation and edits.

### Field-tested guidance
- **Provide targeted context.** Paste or reference *specific* files, paths, or symbols. Avoid noisy project-wide dumps.
- **Set explicit acceptance criteria.** E.g., “All changes pass `pytest -q`, preserve public API, update docs.”
- **Iterate rapidly.** Leverage speed for refine–test–refine cycles. Quote failing stack traces; request diffs.
- **Prefer native tool-calls.** Avoid XML/DIY function-call conventions—use first-party tool calling.
- **Think in agents.** Break tasks into finder→planner→editor→tester with explicit tool affordances.
- **Cache-aware prompts.** Keep prefixes stable to maximize retrieval/caching performance.

### Coding-task scaffold
```
SYSTEM: Senior Staff Engineer AI pair-programmer.
- Objective: implement X with constraints Y.
- Tools available: search(code), read(file), write(file), run(cmd), test().
- Output: minimal diffs + rationale + follow-up checklist.

USER: <Context blocks: FILE: path, SNIPPET: …, TEST: …>
USER: Task: …
USER: Quality bar: lint=pass, tests={…}, perf<=Xms, sec={no secrets, validate inputs}.
USER: Plan then act with tool-calls. Ask for missing inputs explicitly.
```

---

## Appendix: Prompt Patterns & Checklists

### Universal patterns
- **RACI-style role priming:** define the assistant’s Role, Authority, Constraints, Interfaces.
- **Socratic decomposition:** ask the model to list unknowns/assumptions, then resolve them.
- **Dual-pass generation:** *draft → self-critique → final* with explicit success criteria.
- **Output contracts:** JSON schema or regex templates; fail closed on invalid shape.

### Red-team your prompts
- Add adversarial examples (ambiguous, misleading, oversized inputs).
- Require citations for claims drawn from external sources.
- Ban unsupported actions (“never write files outside /workspace”).
- Bound cost/latency: “If tokens > N, summarize then ask.”

---

### License & Attribution
This companion is original work derived from publicly available xAI Guides and augmented with additional best practices. For up-to-date and canonical instructions, always refer to the official docs.
