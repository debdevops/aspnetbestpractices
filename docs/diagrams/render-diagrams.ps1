# Renders Mermaid diagrams in docs/diagrams to PNG using mmdc
# Requires: npm i -g @mermaid-js/mermaid-cli

$diagrams = @(
    @{ input = 'docs/diagrams/auth-flow.mmd'; output = 'docs/diagrams/auth-flow.png' },
    @{ input = 'docs/diagrams/idempotency-flow.mmd'; output = 'docs/diagrams/idempotency-flow.png' },
    @{ input = 'docs/diagrams/csp-flow.mmd'; output = 'docs/diagrams/csp-flow.png' }
)

foreach ($d in $diagrams) {
    Write-Host "Rendering $($d.input) -> $($d.output)"
    mmdc -i $d.input -o $d.output -w 2560 -H 1440
}
