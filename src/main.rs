use macroquad::prelude::*;
use ::rand::Rng;

const NEIGHBOR_RADIUS: f32 = 30.0;
const CELL_RADIUS: f32 = 4.0;
const INITIAL_CELLS: usize = 100;
const WIDTH: f32 = 800.0;
const HEIGHT: f32 = 600.0;
const SPEED: f32 = 20.0;

#[derive(Clone, Copy, Debug)]
struct Cell {
    x: f32,
    y: f32,
    vx: f32,
    vy: f32,
}

impl Cell {
    fn new(x: f32, y: f32) -> Self {
        let mut rng = ::rand::thread_rng();
        let angle = rng.gen_range(0.0..std::f32::consts::TAU);
        Cell {
            x,
            y,
            vx: angle.cos() * SPEED,
            vy: angle.sin() * SPEED,
        }
    }

    fn distance_to(&self, other: &Cell) -> f32 {
        let dx = self.x - other.x;
        let dy = self.y - other.y;
        (dx * dx + dy * dy).sqrt()
    }

    fn distance_to_point(&self, x: f32, y: f32) -> f32 {
        let dx = self.x - x;
        let dy = self.y - y;
        (dx * dx + dy * dy).sqrt()
    }

    fn update(&mut self, dt: f32) {
        self.x += self.vx * dt;
        self.y += self.vy * dt;

        // Wrap around screen edges
        if self.x < 0.0 {
            self.x += WIDTH;
        } else if self.x > WIDTH {
            self.x -= WIDTH;
        }

        if self.y < 0.0 {
            self.y += HEIGHT;
        } else if self.y > HEIGHT {
            self.y -= HEIGHT;
        }
    }
}

struct World {
    cells: Vec<Cell>,
}

impl World {
    fn new() -> Self {
        let mut rng = ::rand::thread_rng();
        let mut cells = Vec::new();

        for _ in 0..INITIAL_CELLS {
            cells.push(Cell::new(
                rng.gen_range(0.0..WIDTH),
                rng.gen_range(0.0..HEIGHT),
            ));
        }

        World { cells }
    }

    fn count_neighbors(&self, cell: &Cell) -> usize {
        self.cells
            .iter()
            .filter(|other| {
                std::ptr::eq(*other, cell) == false
                    && cell.distance_to(other) < NEIGHBOR_RADIUS
            })
            .count()
    }

    fn count_neighbors_at(&self, x: f32, y: f32) -> usize {
        self.cells
            .iter()
            .filter(|cell| cell.distance_to_point(x, y) < NEIGHBOR_RADIUS)
            .count()
    }

    fn draw(&self) {
        for cell in &self.cells {
            draw_circle(cell.x, cell.y, CELL_RADIUS, WHITE);
        }

        // Draw neighbor radius for a few cells (for debugging)
        // Uncomment to visualize neighbor detection
        // if let Some(cell) = self.cells.first() {
        //     draw_circle_lines(cell.x, cell.y, NEIGHBOR_RADIUS, 1.0, Color::new(1.0, 0.0, 0.0, 0.3));
        // }
    }
}

#[macroquad::main("Game of Life - Continuous Space")]
async fn main() {
    let mut world = World::new();
    let mut last_update = 0.0;
    let update_interval = 0.5; // Update every 0.5 seconds

    loop {
        clear_background(BLACK);

        let current_time = get_time() as f32;
        let dt = get_frame_time();

        // Continuous movement
        for cell in &mut world.cells {
            cell.update(dt);
        }

        // Periodic Game of Life updates
        if current_time - last_update > update_interval {
            let mut new_cells = Vec::new();
            let mut cells_to_keep = Vec::new();

            // Check which cells survive
            for cell in &world.cells {
                let neighbors = world.count_neighbors(cell);
                if neighbors == 2 || neighbors == 3 {
                    cells_to_keep.push(*cell);
                }
            }

            // Check for new cell births
            let mut rng = ::rand::thread_rng();
            for cell in &world.cells {
                for _ in 0..4 {
                    let angle = rng.gen_range(0.0..std::f32::consts::TAU);
                    let dist = rng.gen_range(NEIGHBOR_RADIUS * 0.5..NEIGHBOR_RADIUS * 1.5);
                    let x = cell.x + angle.cos() * dist;
                    let y = cell.y + angle.sin() * dist;

                    let neighbors = world.count_neighbors_at(x, y);
                    if neighbors == 3 {
                        let too_close = world.cells.iter().any(|c| c.distance_to_point(x, y) < CELL_RADIUS * 2.0);
                        if !too_close {
                            new_cells.push(Cell::new(x, y));
                        }
                    }
                }
            }

            world.cells = cells_to_keep;
            world.cells.extend(new_cells);

            if world.cells.len() > 500 {
                world.cells.truncate(500);
            }

            last_update = current_time;
        }

        world.draw();

        // Display info
        draw_text(
            &format!("Cells: {}", world.cells.len()),
            10.0,
            20.0,
            20.0,
            GREEN,
        );
        draw_text(
            "Press R to reset",
            10.0,
            40.0,
            20.0,
            GREEN,
        );

        // Reset on R key
        if is_key_pressed(KeyCode::R) {
            world = World::new();
            last_update = current_time;
        }

        next_frame().await
    }
}
