use macroquad::prelude::*;
use ::rand::Rng as _;
use ::rand::thread_rng;
use serde::{Deserialize, Serialize};
use std::fs;

// Constants
const SCREEN_WIDTH: f32 = 1280.0;
const SCREEN_HEIGHT: f32 = 720.0;
const MAX_DOTS: usize = 1000;
const DOT_RADIUS: f32 = 5.0;
const INTERACTION_RADIUS: f32 = 50.0;

// Dot types with different behaviors
#[derive(Debug, Clone, Copy, PartialEq, Serialize, Deserialize)]
enum DotType {
    Classic,      // Basic Game of Life
    Predator,     // Eats other dots
    Prey,         // Runs away from predators
    Absorber,     // Absorbs dots to grow
    Transformer,  // Absorbs dots to change type
    Repulsor,     // Pushes dots away
    Attractor,    // Pulls dots toward it
    Chaser,       // Chases nearby dots
    Protector,    // Protects dots of same type
    Ghost,        // Passes through other dots
    Bouncer,      // Bounces dots away
    Social,       // Prefers company of similar dots
    Grower,       // Grows over time
    Divider,      // Splits into multiple dots
}

impl DotType {
    fn color(&self) -> Color {
        match self {
            DotType::Classic => GRAY,
            DotType::Predator => RED,
            DotType::Prey => YELLOW,
            DotType::Absorber => PURPLE,
            DotType::Transformer => ORANGE,
            DotType::Repulsor => DARKBLUE,
            DotType::Attractor => MAGENTA,
            DotType::Chaser => PINK,
            DotType::Protector => SKYBLUE,
            DotType::Ghost => Color::new(0.8, 0.8, 0.8, 0.5),
            DotType::Bouncer => DARKGREEN,
            DotType::Social => LIME,
            DotType::Grower => BROWN,
            DotType::Divider => Color::new(0.0, 1.0, 1.0, 1.0),
        }
    }

    fn all_types() -> Vec<DotType> {
        vec![
            DotType::Classic,
            DotType::Predator,
            DotType::Prey,
            DotType::Absorber,
            DotType::Transformer,
            DotType::Repulsor,
            DotType::Attractor,
            DotType::Chaser,
            DotType::Protector,
            DotType::Ghost,
            DotType::Bouncer,
            DotType::Social,
            DotType::Grower,
            DotType::Divider,
        ]
    }
}

// Configuration for dot behaviors
#[derive(Debug, Clone, Serialize, Deserialize)]
struct DotConfig {
    max_speed: f32,
    attraction_strength: f32,
    repulsion_strength: f32,
    friction: f32,
    bounce_damping: f32,
    interaction_radius: f32,
    eating_radius: f32,
    growth_rate: f32,
    divide_size: f32,
}

impl Default for DotConfig {
    fn default() -> Self {
        Self {
            max_speed: 5.0,
            attraction_strength: 0.5,
            repulsion_strength: 2.0,
            friction: 0.98,
            bounce_damping: 0.8,
            interaction_radius: INTERACTION_RADIUS,
            eating_radius: 15.0,
            growth_rate: 0.01,
            divide_size: 20.0,
        }
    }
}

// Physics properties for each dot
#[derive(Debug, Clone)]
struct Dot {
    position: Vec2,
    velocity: Vec2,
    acceleration: Vec2,
    dot_type: DotType,
    radius: f32,
    mass: f32,
    spin: f32,
    alive: bool,
    age: f32,
    energy: f32,
}

impl Dot {
    fn new(x: f32, y: f32, dot_type: DotType) -> Self {
        Self {
            position: vec2(x, y),
            velocity: vec2(0.0, 0.0),
            acceleration: vec2(0.0, 0.0),
            dot_type,
            radius: DOT_RADIUS,
            mass: 1.0,
            spin: 0.0,
            alive: true,
            age: 0.0,
            energy: 100.0,
        }
    }

    fn random(dot_type: DotType) -> Self {
        let mut rng = thread_rng();
        let mut dot = Self::new(
            rng.gen_range(50.0..SCREEN_WIDTH - 50.0),
            rng.gen_range(50.0..SCREEN_HEIGHT - 50.0),
            dot_type,
        );
        dot.velocity = vec2(
            rng.gen_range(-2.0..2.0),
            rng.gen_range(-2.0..2.0),
        );
        dot
    }

    fn apply_force(&mut self, force: Vec2) {
        self.acceleration += force / self.mass;
    }

    fn update(&mut self, config: &DotConfig, dt: f32) {
        if !self.alive {
            return;
        }

        // Update velocity and position
        self.velocity += self.acceleration * dt;

        // Apply friction
        self.velocity *= config.friction;

        // Limit max speed
        let speed = self.velocity.length();
        if speed > config.max_speed {
            self.velocity = self.velocity.normalize() * config.max_speed;
        }

        self.position += self.velocity * dt;

        // Bounce off walls
        if self.position.x < self.radius {
            self.position.x = self.radius;
            self.velocity.x *= -config.bounce_damping;
            self.spin = self.velocity.y * 0.1;
        }
        if self.position.x > SCREEN_WIDTH - self.radius {
            self.position.x = SCREEN_WIDTH - self.radius;
            self.velocity.x *= -config.bounce_damping;
            self.spin = -self.velocity.y * 0.1;
        }
        if self.position.y < self.radius {
            self.position.y = self.radius;
            self.velocity.y *= -config.bounce_damping;
            self.spin = -self.velocity.x * 0.1;
        }
        if self.position.y > SCREEN_HEIGHT - self.radius {
            self.position.y = SCREEN_HEIGHT - self.radius;
            self.velocity.y *= -config.bounce_damping;
            self.spin = self.velocity.x * 0.1;
        }

        // Update spin
        self.spin *= 0.95;

        // Reset acceleration
        self.acceleration = vec2(0.0, 0.0);

        // Update age and energy
        self.age += dt;
        self.energy -= 0.1 * dt;

        // Type-specific updates
        match self.dot_type {
            DotType::Grower => {
                self.radius += config.growth_rate * dt;
                self.mass = self.radius * self.radius;
            }
            _ => {}
        }

        // Die if no energy
        if self.energy <= 0.0 {
            self.alive = false;
        }
    }

    fn draw(&self, show_aura: bool) {
        if !self.alive {
            return;
        }

        let color = self.dot_type.color();

        // Draw aura of influence
        if show_aura {
            let aura_color = Color::new(color.r, color.g, color.b, 0.1);
            draw_circle(
                self.position.x,
                self.position.y,
                INTERACTION_RADIUS,
                aura_color,
            );
        }

        // Draw the dot
        draw_circle(self.position.x, self.position.y, self.radius, color);

        // Draw velocity vector (for debugging)
        if show_aura {
            draw_line(
                self.position.x,
                self.position.y,
                self.position.x + self.velocity.x * 5.0,
                self.position.y + self.velocity.y * 5.0,
                1.0,
                WHITE,
            );
        }
    }

    fn distance_to(&self, other: &Dot) -> f32 {
        self.position.distance(other.position)
    }

    fn can_eat(&self, other: &Dot) -> bool {
        match self.dot_type {
            DotType::Predator => {
                matches!(other.dot_type, DotType::Prey | DotType::Classic)
                    && self.radius >= other.radius
            }
            DotType::Absorber => self.radius >= other.radius * 0.9,
            _ => false,
        }
    }
}

// Game state
struct GameState {
    dots: Vec<Dot>,
    config: DotConfig,
    selected_type: DotType,
    paused: bool,
    show_aura: bool,
    game_of_life_mode: bool,
    frame_count: u64,
}

impl GameState {
    fn new() -> Self {
        Self {
            dots: Vec::new(),
            config: DotConfig::default(),
            selected_type: DotType::Classic,
            paused: false,
            show_aura: true,
            game_of_life_mode: false,
            frame_count: 0,
        }
    }

    fn add_dot(&mut self, x: f32, y: f32) {
        if self.dots.len() < MAX_DOTS {
            self.dots.push(Dot::new(x, y, self.selected_type));
        }
    }

    fn seed_random(&mut self, count: usize, _seed: Option<u64>) {
        // Seed parameter available for future use with explicit seeding

        self.dots.clear();

        let types = if self.game_of_life_mode {
            vec![DotType::Classic]
        } else {
            DotType::all_types()
        };

        let mut rng = thread_rng();
        for _ in 0..count {
            let dot_type = types[rng.gen_range(0..types.len())];
            self.dots.push(Dot::random(dot_type));
        }
    }

    fn apply_interactions(&mut self) {
        let config = self.config.clone();

        // Collect interaction forces
        let mut forces: Vec<Vec2> = vec![vec2(0.0, 0.0); self.dots.len()];
        let mut to_remove: Vec<usize> = Vec::new();
        let mut to_add: Vec<Dot> = Vec::new();

        for i in 0..self.dots.len() {
            if !self.dots[i].alive {
                continue;
            }

            for j in (i + 1)..self.dots.len() {
                if !self.dots[j].alive {
                    continue;
                }

                let distance = self.dots[i].distance_to(&self.dots[j]);

                // Skip if too far
                if distance > config.interaction_radius {
                    continue;
                }

                let direction = (self.dots[j].position - self.dots[i].position).normalize();

                // Collision detection
                let min_distance = self.dots[i].radius + self.dots[j].radius;
                if distance < min_distance && distance > 0.1 {
                    // Check for eating
                    if self.dots[i].can_eat(&self.dots[j]) && distance < config.eating_radius {
                        to_remove.push(j);
                        // Absorb energy and potentially grow
                        if self.dots[i].dot_type == DotType::Absorber {
                            self.dots[i].radius += self.dots[j].radius * 0.2;
                            self.dots[i].mass = self.dots[i].radius * self.dots[i].radius;
                        }
                        self.dots[i].energy += self.dots[j].energy * 0.5;
                        continue;
                    } else if self.dots[j].can_eat(&self.dots[i]) && distance < config.eating_radius {
                        to_remove.push(i);
                        if self.dots[j].dot_type == DotType::Absorber {
                            self.dots[j].radius += self.dots[i].radius * 0.2;
                            self.dots[j].mass = self.dots[j].radius * self.dots[j].radius;
                        }
                        self.dots[j].energy += self.dots[i].energy * 0.5;
                        continue;
                    }

                    // Physical collision (unless ghost)
                    if self.dots[i].dot_type != DotType::Ghost && self.dots[j].dot_type != DotType::Ghost {
                        let overlap = min_distance - distance;
                        let separation = direction * overlap * 0.5;

                        forces[i] -= separation;
                        forces[j] += separation;

                        // Elastic collision with spin
                        let relative_velocity = self.dots[i].velocity - self.dots[j].velocity;
                        let impulse = direction * relative_velocity.dot(direction) * config.bounce_damping;

                        forces[i] -= impulse * self.dots[j].mass;
                        forces[j] += impulse * self.dots[i].mass;
                    }
                }

                // Apply type-specific forces
                self.apply_type_forces(i, j, distance, direction, &mut forces, &mut to_add);
            }
        }

        // Apply collected forces
        for (i, force) in forces.iter().enumerate() {
            if self.dots[i].alive {
                self.dots[i].apply_force(*force);
            }
        }

        // Remove eaten dots (in reverse to maintain indices)
        to_remove.sort_unstable();
        to_remove.dedup();
        for &i in to_remove.iter().rev() {
            self.dots[i].alive = false;
        }

        // Add new dots
        for dot in to_add {
            if self.dots.len() < MAX_DOTS {
                self.dots.push(dot);
            }
        }

        // Clean up dead dots periodically
        if self.frame_count % 60 == 0 {
            self.dots.retain(|d| d.alive);
        }
    }

    fn apply_type_forces(
        &self,
        i: usize,
        j: usize,
        distance: f32,
        direction: Vec2,
        forces: &mut Vec<Vec2>,
        new_dots: &mut Vec<Dot>,
    ) {
        let dot_i = &self.dots[i];
        let dot_j = &self.dots[j];
        let config = &self.config;

        match dot_i.dot_type {
            DotType::Attractor => {
                let force = direction * config.attraction_strength / distance.max(1.0);
                forces[j] += force;
            }
            DotType::Repulsor => {
                let force = direction * config.repulsion_strength / distance.max(1.0);
                forces[j] -= force;
            }
            DotType::Chaser => {
                if dot_j.dot_type != dot_i.dot_type {
                    let force = direction * config.attraction_strength * 2.0 / distance.max(1.0);
                    forces[i] += force;
                }
            }
            DotType::Prey => {
                if dot_j.dot_type == DotType::Predator {
                    let force = direction * config.repulsion_strength * 3.0 / distance.max(1.0);
                    forces[i] -= force;
                }
            }
            DotType::Social => {
                if dot_j.dot_type == dot_i.dot_type {
                    let force = direction * config.attraction_strength * 1.5 / distance.max(1.0);
                    forces[i] += force;
                }
            }
            DotType::Protector => {
                if dot_j.dot_type == dot_i.dot_type && distance < config.interaction_radius * 0.7 {
                    // Push away predators from protected dots
                    for k in 0..self.dots.len() {
                        if self.dots[k].dot_type == DotType::Predator {
                            let dist_to_predator = dot_j.distance_to(&self.dots[k]);
                            if dist_to_predator < config.interaction_radius {
                                let dir_to_predator = (self.dots[k].position - dot_j.position).normalize();
                                forces[k] -= dir_to_predator * config.repulsion_strength * 2.0;
                            }
                        }
                    }
                }
            }
            DotType::Divider => {
                if dot_i.radius > config.divide_size && self.frame_count % 120 == 0 {
                    // Create offspring
                    let mut offspring = Dot::new(
                        dot_i.position.x + thread_rng().gen_range(-10.0..10.0),
                        dot_i.position.y + thread_rng().gen_range(-10.0..10.0),
                        DotType::Divider,
                    );
                    offspring.velocity = vec2(
                        thread_rng().gen_range(-2.0..2.0),
                        thread_rng().gen_range(-2.0..2.0),
                    );
                    new_dots.push(offspring);
                }
            }
            DotType::Bouncer => {
                if distance < config.interaction_radius * 0.5 {
                    let force = direction * config.repulsion_strength * 5.0 / distance.max(1.0);
                    forces[j] += force;
                    forces[i] -= force * 0.5;
                }
            }
            _ => {}
        }

        // Symmetric forces
        match dot_j.dot_type {
            DotType::Attractor => {
                let force = direction * config.attraction_strength / distance.max(1.0);
                forces[i] -= force;
            }
            DotType::Repulsor => {
                let force = direction * config.repulsion_strength / distance.max(1.0);
                forces[i] += force;
            }
            _ => {}
        }
    }

    fn update(&mut self, dt: f32) {
        if self.paused {
            return;
        }

        self.frame_count += 1;

        // Apply interactions between dots
        self.apply_interactions();

        // Update each dot
        for dot in self.dots.iter_mut() {
            dot.update(&self.config, dt);
        }

        // Game of Life mode (classic cellular automaton in continuous space)
        if self.game_of_life_mode && self.frame_count % 30 == 0 {
            self.apply_game_of_life_rules();
        }
    }

    fn apply_game_of_life_rules(&mut self) {
        let mut to_add = Vec::new();
        let mut to_remove = Vec::new();

        for i in 0..self.dots.len() {
            if !self.dots[i].alive || self.dots[i].dot_type != DotType::Classic {
                continue;
            }

            // Count neighbors within interaction radius
            let mut neighbors = 0;
            for j in 0..self.dots.len() {
                if i != j && self.dots[j].alive && self.dots[j].dot_type == DotType::Classic {
                    let distance = self.dots[i].distance_to(&self.dots[j]);
                    if distance < self.config.interaction_radius {
                        neighbors += 1;
                    }
                }
            }

            // Classic Game of Life rules: 2-3 neighbors survive, 3 creates new
            if neighbors < 2 || neighbors > 3 {
                to_remove.push(i);
            } else if neighbors == 3 {
                // Try to create new dot nearby
                let mut rng = thread_rng();
                let offset_x = rng.gen_range(-20.0..20.0);
                let offset_y = rng.gen_range(-20.0..20.0);
                to_add.push(Dot::new(
                    self.dots[i].position.x + offset_x,
                    self.dots[i].position.y + offset_y,
                    DotType::Classic,
                ));
            }
        }

        // Apply changes
        for &i in to_remove.iter().rev() {
            self.dots[i].alive = false;
        }
        for dot in to_add {
            if self.dots.len() < MAX_DOTS {
                self.dots.push(dot);
            }
        }
    }

    fn draw(&self) {
        for dot in &self.dots {
            dot.draw(self.show_aura);
        }
    }

    fn save_config(&self, filename: &str) -> Result<(), Box<dyn std::error::Error>> {
        let json = serde_json::to_string_pretty(&self.config)?;
        fs::write(filename, json)?;
        Ok(())
    }

    fn load_config(&mut self, filename: &str) -> Result<(), Box<dyn std::error::Error>> {
        let json = fs::read_to_string(filename)?;
        self.config = serde_json::from_str(&json)?;
        Ok(())
    }
}

fn window_conf() -> Conf {
    Conf {
        window_title: "DotGame - Game of Life Evolution".to_owned(),
        window_width: SCREEN_WIDTH as i32,
        window_height: SCREEN_HEIGHT as i32,
        window_resizable: false,
        ..Default::default()
    }
}

#[macroquad::main(window_conf)]
async fn main() {
    let mut game = GameState::new();
    let mut selected_type_index = 0;
    let all_types = DotType::all_types();

    // Try to load config
    let _ = game.load_config("dotgame_config.json");

    // Seed with some initial dots
    game.seed_random(50, None);

    loop {
        // Input handling
        if is_key_pressed(KeyCode::Space) {
            game.paused = !game.paused;
        }

        if is_key_pressed(KeyCode::A) {
            game.show_aura = !game.show_aura;
        }

        if is_key_pressed(KeyCode::G) {
            game.game_of_life_mode = !game.game_of_life_mode;
        }

        if is_key_pressed(KeyCode::C) {
            game.dots.clear();
        }

        if is_key_pressed(KeyCode::R) {
            game.seed_random(100, Some(thread_rng().gen()));
        }

        if is_key_pressed(KeyCode::S) {
            let _ = game.save_config("dotgame_config.json");
        }

        if is_key_pressed(KeyCode::L) {
            let _ = game.load_config("dotgame_config.json");
        }

        // Cycle through dot types
        if is_key_pressed(KeyCode::Tab) {
            selected_type_index = (selected_type_index + 1) % all_types.len();
            game.selected_type = all_types[selected_type_index];
        }

        if is_key_pressed(KeyCode::Key1) {
            game.config.max_speed = (game.config.max_speed + 1.0).min(20.0);
        }
        if is_key_pressed(KeyCode::Key2) {
            game.config.max_speed = (game.config.max_speed - 1.0).max(1.0);
        }

        // Mouse input for placing dots
        if is_mouse_button_down(MouseButton::Left) {
            let (x, y) = mouse_position();
            game.add_dot(x, y);
        }

        if is_mouse_button_down(MouseButton::Right) {
            // Remove dots near mouse
            let (x, y) = mouse_position();
            for dot in &mut game.dots {
                if dot.position.distance(vec2(x, y)) < 20.0 {
                    dot.alive = false;
                }
            }
        }

        // Update game state (60 fps target, dt = 1.0 for simplicity)
        game.update(1.0);

        // Rendering
        clear_background(BLACK);

        game.draw();

        // Draw UI
        let ui_text = format!(
            "FPS: {:.0} | Dots: {} | Type: {:?} (Tab to change)\n\
             Space: Pause {} | A: Aura {} | G: Game of Life {} | C: Clear | R: Random\n\
             1/2: Speed +/- | S: Save Config | L: Load Config\n\
             Left Click: Add | Right Click: Remove",
            get_fps(),
            game.dots.iter().filter(|d| d.alive).count(),
            game.selected_type,
            if game.paused { "⏸" } else { "▶" },
            if game.show_aura { "✓" } else { "✗" },
            if game.game_of_life_mode { "✓" } else { "✗" },
        );

        draw_text(&ui_text, 10.0, 20.0, 20.0, WHITE);

        // Draw selected type indicator
        draw_circle(SCREEN_WIDTH - 50.0, 50.0, 20.0, game.selected_type.color());
        draw_text("Selected", SCREEN_WIDTH - 100.0, 80.0, 16.0, WHITE);

        next_frame().await;
    }
}
