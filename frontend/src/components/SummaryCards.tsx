import { Card, CardContent, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

export interface SummaryCard {
  label: string;
  value: ReactNode;
  color?: string;
}

interface Props {
  cards: SummaryCard[];
}

export default function SummaryCards({ cards }: Props) {
  return (
    <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: 'wrap', gap: 1 }}>
      {cards.map((card) => (
        <Card key={card.label} sx={{ minWidth: 180, flex: '1 1 180px' }}>
          <CardContent>
            <Typography variant="body2" color="text.secondary">
              {card.label}
            </Typography>
            <Typography variant="h5" sx={{ color: card.color ?? 'text.primary', fontWeight: 600 }}>
              {card.value}
            </Typography>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}
